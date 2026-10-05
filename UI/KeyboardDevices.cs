using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Windows.Forms;

namespace KeyboardChatterBlocker
{
    /// <summary>一个键盘设备。设备路径是稳定标识，配置里存的就是它。</summary>
    internal sealed class KeyboardDevice
    {
        /// <summary>设备路径（小写）。</summary>
        public string Path;
        /// <summary>界面上显示的名字。</summary>
        public string Label;

        public override string ToString()
        {
            return Label;
        }
    }

    /// <summary>
    /// 主进程这一侧的设备跟踪：**只是读者**。
    ///
    /// <para>
    /// ⚠ 本进程**绝不能自己注册 Raw Input**。实测（可复现）：一旦在进程内调用
    /// <c>RegisterRawInputDevices</c>，本进程的低级键盘钩子会彻底停止被调用 ——
    /// 钩子句柄正常、重装钩子无效、换独立线程也无效，而注销注册后立刻恢复。
    /// 对靠钩子拦截抖动的本程序来说等于功能瘫痪。
    /// </para>
    /// <para>
    /// 所以真正的 Raw Input 交给独立进程做（<see cref="DeviceTrackerHost"/>，
    /// 同一份 exe 用 <c>--device-tracker</c> 开关启动）。它把「最近一次按键来自哪把键盘」
    /// 写进共享内存，这边定时读出来。
    /// </para>
    /// <para>
    /// <b>滞后一次事件</b>这一点仍然存在：钩子回调永远早于 Raw Input 到达，
    /// 所以判断当前按键用的是「上一个按键的设备」。跨键盘切换后的第一次按键可能判错，
    /// 之后立刻纠正。
    /// </para>
    /// </summary>
    internal static class KeyboardDevices
    {
        /// <summary>轮询间隔（毫秒）。键按下到设备被认出来最多慢这么久。</summary>
        private const int PollIntervalMs = 25;

        private static MemoryMappedFile _map;
        private static Process _helper;
        private static Timer _poll;
        private static readonly List<KeyboardDevice> _active = new List<KeyboardDevice>();
        private static readonly Dictionary<string, KeyboardDevice> _byPath = new Dictionary<string, KeyboardDevice>();
        private static string _currentPath;
        private static long _lastKeySeq = -1;

        /// <summary>设备列表发生变化（又有新键盘被按到）。</summary>
        public static event Action DevicesChanged;

        /// <summary>收到某个设备的按键。识别功能用。</summary>
        public static event Action<KeyboardDevice> KeyFromDevice;

        /// <summary>已经按出过按键的键盘，按首次出现的顺序。</summary>
        public static IList<KeyboardDevice> Active { get { return _active; } }

        /// <summary>开始跟踪：拉起辅助进程并开始轮询共享内存。</summary>
        public static void Start()
        {
            if (_poll != null)
            {
                return;
            }
            try
            {
                _helper = Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = DeviceTrackerHost.Switch + " " + Process.GetCurrentProcess().Id,
                    UseShellExecute = false,
                });
            }
            catch (Exception)
            {
                // 起不来就没有设备识别，功能退化成「所有键盘都拦」—— 不影响拦截本身
            }
            try
            {
                _map = MemoryMappedFile.CreateOrOpen(DeviceTrackerIpc.MapName, DeviceTrackerIpc.Capacity);
            }
            catch (Exception)
            {
                _map = null;
            }
            _poll = new Timer { Interval = PollIntervalMs };
            _poll.Tick += (s, e) => Poll();
            _poll.Start();
        }

        /// <summary>停止跟踪并收掉辅助进程。</summary>
        public static void Stop()
        {
            if (_poll != null)
            {
                _poll.Stop();
                _poll.Dispose();
                _poll = null;
            }
            if (_map != null)
            {
                _map.Dispose();
                _map = null;
            }
            if (_helper != null)
            {
                try
                {
                    if (!_helper.HasExited) { _helper.Kill(); }
                }
                catch (Exception) { }
                _helper = null;
            }
        }

        /// <summary>
        /// 当前这次按键是否来自「参与拦截」的键盘。跑在输入路径上，必须极快。
        /// <para>白名单为空 = 所有键盘都参与（默认行为）。</para>
        /// </summary>
        public static bool IsCurrentDeviceEnabled()
        {
            List<string> enabled;
            try
            {
                enabled = Program.Blocker != null ? Program.Blocker.EnabledKeyboards : null;
            }
            catch (Exception)
            {
                return true;
            }
            if (enabled == null || enabled.Count == 0)
            {
                return true;   // 默认：不筛
            }
            string path = _currentPath;
            if (path == null)
            {
                return true;   // 还不知道是哪个键盘 —— 宁可多拦也不要漏拦
            }
            return enabled.Contains(path);
        }

        /// <summary>读一次共享内存。序号前后一致才算读完整。</summary>
        private static void Poll()
        {
            if (_map == null)
            {
                return;
            }
            try
            {
                int seqBefore, count, current;
                long keySeq;
                List<string> paths = new List<string>();
                using (MemoryMappedViewStream view = _map.CreateViewStream())
                using (BinaryReader reader = new BinaryReader(view, Encoding.Unicode))
                {
                    view.Position = 0;
                    seqBefore = reader.ReadInt32();
                    view.Position = 4;
                    count = reader.ReadInt32();
                    current = reader.ReadInt32();
                    keySeq = reader.ReadInt64();
                    if (count < 0 || count > 64)
                    {
                        return;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        paths.Add(reader.ReadString());
                    }
                    view.Position = 0;
                    if (reader.ReadInt32() != seqBefore)
                    {
                        return;   // 读到一半被写了，这一轮丢弃
                    }
                }

                _currentPath = (current >= 0 && current < paths.Count) ? paths[current] : null;

                bool changed = false;
                foreach (string path in paths)
                {
                    if (_byPath.ContainsKey(path))
                    {
                        continue;
                    }
                    KeyboardDevice device = new KeyboardDevice { Path = path, Label = BuildLabel(path) };
                    _byPath[path] = device;
                    _active.Add(device);
                    changed = true;
                }
                if (changed)
                {
                    Action handler = DevicesChanged;
                    if (handler != null) { handler(); }
                }

                // 「刚刚又按了一下」—— 识别功能靠这个把按键和设备对上
                if (_lastKeySeq >= 0 && keySeq != _lastKeySeq && _currentPath != null)
                {
                    KeyboardDevice device;
                    if (_byPath.TryGetValue(_currentPath, out device))
                    {
                        Action<KeyboardDevice> onKey = KeyFromDevice;
                        if (onKey != null) { onKey(device); }
                    }
                }
                _lastKeySeq = keySeq;
            }
            catch (Exception)
            {
                // 共享内存异常不能影响界面
            }
        }

        /// <summary>设备路径 → 界面显示名。</summary>
        public static string BuildLabel(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "未知键盘";
            }
            string s = path;
            int brace = s.IndexOf("#{", StringComparison.Ordinal);
            if (brace > 0)
            {
                s = s.Substring(0, brace);
            }
            s = s.TrimStart('\\', '?', '\\');
            string[] parts = s.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            // 形如 VID_1A2C&PID_7FFF 的那一段最有辨识度
            foreach (string part in parts)
            {
                if (part.StartsWith("VID_", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Replace("&", " & ");
                }
            }
            // 笔记本内置键盘走 ACPI，没有 VID/PID
            if (parts.Length >= 2)
            {
                return parts[0] + " " + parts[1];
            }
            return parts.Length > 0 ? parts[0] : s;
        }
    }
}
