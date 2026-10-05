using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// Keyboard interceptor underlying hook tool, based on https://blogs.msdn.microsoft.com/toub/2006/05/03/low-level-keyboard-hook-in-c/
    /// </summary>
    public class KeyboardInterceptor : IDisposable
    {
        /// <summary>
        /// Hook ID for keyboard intercept.
        /// </summary>
        public const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Hook ID for mouse intercept.
        /// </summary>
        public const int WH_MOUSE_LL = 14;

        /// <summary>
        /// Key state change Windows message.
        /// </summary>
        public const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105;

        /// <summary>
        /// Mouse state change Windows message.
        /// </summary>
        public const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202,
            WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205,
            WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208,
            WM_XBUTTONDOWN = 0x020B, WM_XBUTTONUP = 0x020C,
            WM_MOUSEWHEEL = 0x020A;

        /// <summary>
        /// An array of falses, except for the WParam values that are handled by this program.
        /// This exists as an optimization structure to reduce potential input delay caused by this running.
        /// </summary>
        public static bool[] HANDLED_WPARAMS = new bool[1024];

        static KeyboardInterceptor()
        {
            foreach (int wparam in new[] { WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, WM_SYSKEYUP,
                    WM_LBUTTONDOWN, WM_LBUTTONUP, WM_RBUTTONDOWN, WM_RBUTTONUP,
                    WM_MBUTTONDOWN, WM_MBUTTONUP, WM_XBUTTONDOWN, WM_XBUTTONUP })
            {
                HANDLED_WPARAMS[wparam] = true;
            }
        }

        /// <summary>
        /// Reference to the keyboard Hook Callback.
        /// </summary>
        public LowLevelKeyboardProc KeyboardProcCallback;

        /// <summary>
        /// Reference to the mouse Hook Callback.
        /// </summary>
        public LowLevelKeyboardProc MouseProcCallback;

        /// <summary>
        /// The relevant <see cref="KeyBlocker"/>.
        /// </summary>
        public KeyBlocker KeyBlockHandler;

        /// <summary>
        /// 每一个键盘事件（按下/抬起）都会触发，带上该事件的判定结果。
        /// <para>
        /// 参数：按键、是否为按下、是否被放行。<b>无论屏蔽是否启用都会触发</b>，
        /// 因此键盘测试页在程序处于停用状态时同样能工作。
        /// </para>
        /// <para>
        /// ⚠ 回调跑在装钩子的线程上（本程序中即 UI 线程），且处在输入路径上，
        /// 必须极快返回 —— 只做状态记录，不要在里面对控件做重建。
        /// </para>
        /// </summary>
        public Action<Keys, bool, bool> KeyEvent;

        /// <summary>
        /// 当前这个按键是否来自「参与拦截」的键盘。由 UI 层按设备白名单设置；
        /// <b>未设置时一律当作 true</b>（= 所有键盘都参与拦截，即默认行为）。
        /// <para>
        /// 返回 false 时该事件的按下/抬起都会被直接放行，而且**不进入记账**
        /// （不调用 <see cref="KeyBlocker.AllowKeyDown"/> / <see cref="KeyBlocker.AllowKeyUp"/>）——
        /// 否则被排除键盘的状态会残留在 KeyBlocker 里。
        /// </para>
        /// <para>
        /// ⚠ 和 <see cref="KeyEvent"/> 一样跑在输入路径上，实现必须极快返回。
        /// </para>
        /// </summary>
        public Func<bool> CurrentDeviceAllowed;

        /// <summary>
        /// The current keyboard hook ID.
        /// </summary>
        public IntPtr KeyboardHookID = IntPtr.Zero;

        /// <summary>
        /// The current mouse hook ID.
        /// </summary>
        public IntPtr MouseHookID = IntPtr.Zero;

        public KeyboardInterceptor(KeyBlocker blocker)
        {
            blocker.Interceptor = this;
            KeyBlockHandler = blocker;
            KeyboardProcCallback = KeyboardHookCallback;
            MouseProcCallback = MouseHookCallback;
            EnableKeyboardHook();
        }

        /// <summary>
        /// Enables the keyboard hook (if not already enabled).
        /// </summary>
        public void EnableKeyboardHook()
        {
            if (KeyboardHookID == IntPtr.Zero)
            {
                KeyboardHookID = SetKeyboardHook(KeyboardProcCallback);
            }
        }

        /// <summary>
        /// Enables the mouse hook (if not already enabled).
        /// </summary>
        public void EnableMouseHook()
        {
            if (MouseHookID == IntPtr.Zero)
            {
                MouseHookID = SetMouseHook(MouseProcCallback);
            }
        }

        /// <summary>
        /// Sets a keyboard hook for the current process onto the global Windows hook system.
        /// </summary>
        /// <param name="proc">The keyboard callback proc to use.</param>
        public static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// Sets a mouse hook for the current process onto the global Windows hook system.
        /// </summary>
        /// <param name="proc">The keyboard callback proc to use.</param>
        public static IntPtr SetMouseHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// Delegate type for keyboard callback functions.
        /// </summary>
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// The primary keyboard hook callback.
        /// </summary>
        /// <param name="nCode">The 'n' code (unused).</param>
        /// <param name="wParam">The 'w' parameter (Windows message ID in this case).</param>
        /// <param name="lParam">The 'l' parameter (key pressed in this case).</param>
        /// <returns>The result of the hook callback continuation (or '1' to block).</returns>
        public IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int wParamInt = (int)wParam;
            if (nCode >= 0 && wParamInt < 1024 && HANDLED_WPARAMS[wParamInt])
            {
                bool isDown = wParamInt == WM_KEYDOWN || wParamInt == WM_SYSKEYDOWN;
                if (isDown || wParamInt == WM_KEYUP || wParamInt == WM_SYSKEYUP)
                {
                    KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    KBDLLHOOKSTRUCTFlags flags = (KBDLLHOOKSTRUCTFlags)hookStruct.flags;
                    if (KeyBlockHandler.ExcludeInjected && (flags.HasFlag(KBDLLHOOKSTRUCTFlags.LLKHF_INJECTED) || flags.HasFlag(KBDLLHOOKSTRUCTFlags.LLKHF_LOWER_IL_INJECTED)))
                    {
                        return CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
                    }
                    Keys key = (Keys)hookStruct.vkCode;
                    // 键盘设备白名单：不在名单里的键盘一律放行，而且完全不记账
                    Func<bool> deviceAllowed = CurrentDeviceAllowed;
                    if (deviceAllowed != null && !deviceAllowed())
                    {
                        // 照样上报，键盘测试页仍然看得到这些事件（只是永远不会被标红）
                        KeyEvent?.Invoke(key, isDown, true);
                        return CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
                    }
                    bool allowed;
                    if (isDown)
                    {
                        // 一并传入扫描码与扩展位：长按救援补发时要用它们合成与真实事件等价的操作
                        allowed = KeyBlockHandler.AllowKeyDown(key, false, hookStruct.scanCode, flags.HasFlag(KBDLLHOOKSTRUCTFlags.LLKHF_EXTENDED));
                    }
                    else
                    {
                        allowed = KeyBlockHandler.AllowKeyUp(key);
                    }
                    // 键盘测试页用：上报每一个事件及它的判定结果。
                    // 放在 return 之前，因此即使屏蔽被关掉（AllowKeyDown 直接放行）也照常上报。
                    KeyEvent?.Invoke(key, isDown, allowed);
                    if (!allowed)
                    {
                        return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// The direction of last mouse wheel delta (positive or negative, as 1 or -1),
        /// used to trigger "wheel_change" detection.
        /// </summary>
        public int LastWheelDirection = 0;

        /// <summary>
        /// The primary mouse hook callback.
        /// </summary>
        /// <param name="nCode">The 'n' code (unused).</param>
        /// <param name="wParam">The 'w' parameter (Windows message ID in this case).</param>
        /// <param name="lParam">The 'l' parameter (key pressed in this case).</param>
        /// <returns>The result of the hook callback continuation (or '1' to block).</returns>
        public IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int wParamInt = (int)wParam;
            if (wParamInt == WM_MOUSEWHEEL)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                short wheelDelta = (short)((hookStruct.mouseData) >> 16);
                int wheelDirection = Math.Sign(wheelDelta);
                if (LastWheelDirection != wheelDirection)
                {
                    bool allow = KeyBlockHandler.AllowKeyDown(KeysHelper.KEY_WHEEL_CHANGE, true);
                    KeyBlockHandler.AllowKeyUp(KeysHelper.KEY_WHEEL_CHANGE);
                    if (!allow)
                    {
                        return (IntPtr)1;
                    }
                    LastWheelDirection = wheelDirection;
                }
            }
            else if (nCode >= 0 && wParamInt < 1024 && HANDLED_WPARAMS[wParamInt])
            {
                bool isDown = wParamInt == WM_LBUTTONDOWN || wParamInt == WM_RBUTTONDOWN || wParamInt == WM_MBUTTONDOWN || wParamInt == WM_XBUTTONDOWN;
                if (isDown || wParamInt == WM_LBUTTONUP || wParamInt == WM_RBUTTONUP || wParamInt == WM_MBUTTONUP || wParamInt == WM_XBUTTONUP || wParamInt == WM_MOUSEWHEEL)
                {
                    Keys key;
                    if (wParamInt == WM_LBUTTONDOWN || wParamInt == WM_LBUTTONUP)
                    {
                        key = KeysHelper.KEY_MOUSE_LEFT;
                    }
                    else if (wParamInt == WM_RBUTTONDOWN || wParamInt == WM_RBUTTONUP)
                    {
                        key = KeysHelper.KEY_MOUSE_RIGHT;
                    }
                    else if (wParamInt == WM_MBUTTONDOWN || wParamInt == WM_MBUTTONUP)
                    {
                        key = KeysHelper.KEY_MOUSE_MIDDLE;
                    }
                    else
                    {
                        MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                        if (hookStruct.mouseData == 0x20000)
                        {
                            key = KeysHelper.KEY_MOUSE_FORWARD;
                        }
                        else
                        {
                            key = KeysHelper.KEY_MOUSE_BACKWARD;
                        }
                    }
                    if (isDown)
                    {
                        if (!KeyBlockHandler.AllowKeyDown(key, true))
                        {
                            return (IntPtr)1;
                        }
                    }
                    else
                    {
                        if (!KeyBlockHandler.AllowKeyUp(key))
                        {
                            return (IntPtr)1;
                        }
                    }
                }
            }
            return CallNextHookEx(MouseHookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Helper struct for mouse event data.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int coordX;
            public int coordY;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Helper struct for keyboard event data.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        
        /// <summary>
        /// Flags for <see cref="KBDLLHOOKSTRUCT"/>
        /// </summary>
        [Flags]
        private enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_LOWER_IL_INJECTED = 0x02,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80
        }

        /// <summary>
        /// External Windows API call. Sets a global Windows hook.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// External Windows API call. Removes a global Windows hook.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// External Windows API call. Continues a hook callback procedure.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// External Windows API call. Gets a handle on a process module.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Disables the mouse hook (to avoid input latency impact).
        /// </summary>
        public void DisableMouseHook()
        {
            if (MouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(MouseHookID);
                MouseHookID = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Disables the keyboard hook (to fully disable when Auto-Disable Programs are used).
        /// </summary>
        public void DisableKeyboardHook()
        {
            if (KeyboardHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(KeyboardHookID);
                KeyboardHookID = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Dispose the object, removing the hook.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            DisableKeyboardHook();
            DisableMouseHook();
        }

        /// <summary>
        /// Destruct the object, removing the hook.
        /// </summary>
        ~KeyboardInterceptor()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose the object, removing the hook.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
