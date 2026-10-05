using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// 「键盘设备跟踪」辅助进程。
    ///
    /// <para>
    /// 为什么要有这个独立进程：低级键盘钩子拿不到设备标识，唯一能区分键盘的途径是
    /// Raw Input 的 <c>WM_INPUT</c>。但**实测发现，进程一旦调用
    /// <c>RegisterRawInputDevices</c>，本进程的低级键盘钩子会彻底停止被调用**
    /// （钩子句柄正常、重装无效、换线程也无效；注销注册后立刻恢复）。
    /// 对靠键盘钩子吃饭的主程序来说这是致命的，所以把 Raw Input 隔离出去。
    /// </para>
    /// <para>
    /// 本进程**不装任何钩子、不显示任何窗口**，只做一件事：把「最近一次按键来自哪把键盘」
    /// 写进共享内存。主进程定时读取（见 <see cref="KeyboardDevices"/>）。
    /// </para>
    /// </summary>
    internal static class DeviceTrackerHost
    {
        /// <summary>命令行开关。</summary>
        public const string Switch = "--device-tracker";

        private const int WM_INPUT = 0x00FF;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER { public uint dwType, dwSize; public IntPtr hDevice, wParam; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE { public ushort usUsagePage, usUsage; public uint dwFlags; public IntPtr hwndTarget; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetRawInputDeviceInfoW(IntPtr hDevice, uint command, StringBuilder data, ref uint size);

        private const uint RIDI_DEVICENAME = 0x20000007;

        /// <summary>这次启动是不是「辅助进程」模式。</summary>
        public static bool IsTrackerInvocation(string[] args)
        {
            return args != null && args.Length >= 1 && args[0] == Switch;
        }

        private static readonly List<string> _paths = new List<string>();
        private static readonly object _lock = new object();
        private static int _current = -1;
        /// <summary>按键计数。每次按下自增，写进共享内存供主进程判断「刚刚又按了一下」。</summary>
        private static long _keySeq;
        private static MemoryMappedFile _map;
        private static RawWindow _window;

        /// <summary>辅助进程入口。参数：<c>--device-tracker &lt;父进程 PID&gt;</c></summary>
        public static void Run(string[] args)
        {
            int parentPid = 0;
            if (args.Length >= 2)
            {
                int.TryParse(args[1], out parentPid);
            }

            // 只允许一个辅助进程：上一个主进程异常退出时可能留下孤儿
            bool createdNew;
            using (Mutex single = new Mutex(true, DeviceTrackerIpc.SingleInstanceMutexName, out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }
                RunCore(parentPid);
            }
        }

        private static void Note(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "tracker.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine);
            }
            catch (Exception) { }
        }

        private static void RunCore(int parentPid)
        {
            Note("启动，父进程=" + parentPid);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Note("未处理异常: " + e.ExceptionObject);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Note("进程退出");
            _map = MemoryMappedFile.CreateOrOpen(DeviceTrackerIpc.MapName, DeviceTrackerIpc.Capacity);
            _window = new RawWindow();
            IntPtr handle = _window.Handle;   // 取得句柄但不 Show —— 窗口不可见，主进程的重复检测不会误认它

            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[]
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01,          // Generic Desktop
                    usUsage = 0x06,              // Keyboard
                    dwFlags = RIDEV_INPUTSINK,   // 不在前台也要收
                    hwndTarget = handle,
                }
            };
            bool ok = RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
            Note("窗口句柄=" + handle + "  RegisterRawInputDevices=" + ok);
            if (!ok)
            {
                return;   // 注册不了就没什么可做的了
            }

            // 父进程一没，立刻退出，不留孤儿
            if (parentPid > 0)
            {
                Thread watchdog = new Thread(() => WatchParent(parentPid));
                watchdog.IsBackground = true;
                watchdog.Start();
            }

            Note("进入消息循环");
            Application.Run(new ApplicationContext());
            Note("消息循环结束");
        }

        /// <summary>
        /// 盯着父进程，它一没就退出。
        /// <para>
        /// ⚠ 这里踩过两个坑，都修掉了：
        /// ① <c>Application.ExitThread()</c> **只对调用它的那个线程生效** —— 在后台线程上调用
        ///    等于什么都没做，消息循环照跑，辅助进程就成了孤儿。必须回到 UI 线程上调用。
        /// ② 不能每轮都用 <c>GetProcessById(pid)</c> 重新查 —— 父进程死后 PID 会被系统复用，
        ///    新查到的就是别的进程了，会永远等下去。**只取一次 Process 对象**，
        ///    它持有句柄，PID 复用也认不错。
        /// </para>
        /// </summary>
        private static void WatchParent(int parentPid)
        {
            Process parent;
            try
            {
                parent = Process.GetProcessById(parentPid);
            }
            catch (ArgumentException)
            {
                ExitHelper("父进程已经不在，直接退出");
                return;
            }
            try
            {
                while (!parent.WaitForExit(1000))
                {
                    // 父进程还活着，继续等
                }
                ExitHelper("父进程退出");
            }
            catch (Exception ex)
            {
                ExitHelper("看门狗异常，退出：" + ex.Message);
            }
        }

        /// <summary>
        /// 把辅助进程关掉。必须回到 UI 线程上结束消息循环；
        /// 万一 UI 线程卡住导致那条路走不通，几秒后直接硬退 —— **绝不能留下孤儿**。
        /// </summary>
        private static void ExitHelper(string reason)
        {
            Note(reason);
            try
            {
                if (_window != null && _window.IsHandleCreated)
                {
                    _window.BeginInvoke(new Action(() => Application.ExitThread()));
                    Thread.Sleep(3000);   // 正常的话这会儿已经退了
                }
            }
            catch (Exception)
            {
                // 落到下面的兜底
            }
            Environment.Exit(0);
        }

        private static string PathOf(IntPtr handle)
        {
            uint size = 0;
            GetRawInputDeviceInfoW(handle, RIDI_DEVICENAME, null, ref size);
            if (size == 0) { return null; }
            StringBuilder buffer = new StringBuilder((int)size + 2);
            if (GetRawInputDeviceInfoW(handle, RIDI_DEVICENAME, buffer, ref size) == uint.MaxValue) { return null; }
            string path = buffer.ToString();
            return string.IsNullOrEmpty(path) ? null : path.ToLowerInvariant();
        }

        private static void OnKeyFrom(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;   // 注入的事件没有设备
            }
            string path = PathOf(handle);
            if (path == null)
            {
                return;
            }
            lock (_lock)
            {
                int index = _paths.IndexOf(path);
                if (index < 0)
                {
                    _paths.Add(path);
                    index = _paths.Count - 1;
                }
                _current = index;
                _keySeq++;
                Publish();
            }
        }

        /// <summary>把当前状态整体写进共享内存。序号最后写，读方用它校验一致性。</summary>
        private static void Publish()
        {
            try
            {
                using (MemoryMappedViewStream view = _map.CreateViewStream())
                using (BinaryWriter writer = new BinaryWriter(view, Encoding.Unicode))
                {
                    view.Position = 4;
                    writer.Write(_paths.Count);
                    writer.Write(_current);
                    writer.Write(_keySeq);   // 按键计数：主进程靠它判断「刚刚又按了一下」
                    foreach (string path in _paths)
                    {
                        writer.Write(path);
                    }
                    writer.Flush();
                    // 序号放最后写：读到相同序号就说明这次读取没跨到一半的写入
                    view.Position = 0;
                    writer.Write(Environment.TickCount);
                    writer.Flush();
                }
            }
            catch (Exception)
            {
                // 共享内存出问题不能影响按键处理
            }
        }

        private sealed class RawWindow : Form
        {
            public RawWindow()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                WindowState = FormWindowState.Minimized;
                Text = "KCB device tracker";
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_INPUT)
                {
                    try
                    {
                        uint size = 0;
                        uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
                        GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref size, headerSize);
                        if (size > 0 && size < 1024)
                        {
                            IntPtr buffer = Marshal.AllocHGlobal((int)size);
                            try
                            {
                                if (GetRawInputData(m.LParam, RID_INPUT, buffer, ref size, headerSize) == size)
                                {
                                    RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                                    ushort flags = (ushort)Marshal.ReadInt16(buffer, (int)headerSize + 2);
                                    if ((flags & 0x01) == 0)   // 只看按下
                                    {
                                        OnKeyFrom(header.hDevice);
                                    }
                                }
                            }
                            finally { Marshal.FreeHGlobal(buffer); }
                        }
                    }
                    catch (Exception)
                    {
                        // 输入路径上不抛异常
                    }
                }
                base.WndProc(ref m);
            }
        }
    }

    /// <summary>主进程与辅助进程之间的共享内存约定。</summary>
    internal static class DeviceTrackerIpc
    {
        public const string MapName = "KeyboardChatterBlocker.DeviceTracker.v1";
        public const string SingleInstanceMutexName = "KeyboardChatterBlocker.DeviceTracker.Mutex.v1";
        public const int Capacity = 64 * 1024;
    }
}
