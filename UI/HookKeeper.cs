using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// 让本程序的键盘钩子尽量待在钩子链的**最前面**。
    ///
    /// <para>
    /// 背景：Windows 的钩子链是「后装的先被调用」，而且任何一个钩子返回非 0 就会截断整条链。
    /// 有些游戏（冰与火就装了，见 <c>Assembly-CSharp-firstpass.dll</c> 的
    /// <c>SetWindowsHookEx</c> / <c>WH_KEYBOARD_LL</c>、<c>Rewired_Windows.dll</c>、
    /// <c>SkyHook.Unity.dll</c>）自己也装低级键盘钩子。
    /// 如果游戏是在本程序之后启动的，它的钩子就排在本程序前面 ——
    /// 一旦它返回非 0，本程序根本收不到按键，也就无从拦截。
    /// </para>
    /// <para>
    /// 现象就是「先开键盘防抖、后开游戏 → 拦不住；反过来或者重启一次键盘防抖 → 好了」。
    /// </para>
    /// <para>
    /// 对策：<see cref="SetWindowsHookEx"/> 永远把新钩子插到链首，所以只要**重装一次**
    /// 就能抢回最前面。这里在前台窗口切换时重装（游戏切到前台的那一刻正是需要抢的时候），
    /// 并留一个低频兜底，防止游戏是在之后某个时刻才装上自己的钩子。
    /// </para>
    /// </summary>
    internal static class HookKeeper
    {
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        /// <summary>兜底重装的间隔（毫秒）。</summary>
        private const int FallbackIntervalMs = 5000;

        /// <summary>前台切换后延迟再抢一次的间隔（毫秒）：有些游戏是拿到前台之后才装钩子。</summary>
        private const int DelayedRetryMs = 600;

        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmod,
            WinEventProc pfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        // 必须留着引用，否则委托被回收后回调会崩
        private static readonly WinEventProc _foregroundProc = OnForegroundChanged;
        private static IntPtr _winEventHook = IntPtr.Zero;
        private static Timer _delayedRetry;
        private static Timer _fallback;

        /// <summary>重装次数，供诊断/验证查看。</summary>
        public static int ReinstallCount;

        /// <summary>开始看护。应在拦截器建好之后调用。</summary>
        public static void Start()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                return;
            }
            _winEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _foregroundProc, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            _delayedRetry = new Timer { Interval = DelayedRetryMs };
            _delayedRetry.Tick += (s, e) =>
            {
                _delayedRetry.Stop();
                Reinstall();
            };

            _fallback = new Timer { Interval = FallbackIntervalMs };
            _fallback.Tick += (s, e) => Reinstall();
            _fallback.Start();
        }

        /// <summary>停止看护。</summary>
        public static void Stop()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }
            if (_fallback != null) { _fallback.Stop(); }
            if (_delayedRetry != null) { _delayedRetry.Stop(); }
        }

        private static void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // 只在顶层窗口切换时处理（子树事件会刷得很凶）
            if (idObject != 0 || idChild != 0)
            {
                return;
            }
            Reinstall();
            // 稍后再抢一次：有的游戏是在拿到前台之后才装自己的钩子
            if (_delayedRetry != null)
            {
                _delayedRetry.Stop();
                _delayedRetry.Start();
            }
        }

        /// <summary>
        /// 重装键盘钩子（鼠标钩子若开着也一并重装），从而回到钩子链最前面。
        /// 中间那一瞬间没有钩子在生效，但只是不拦截，不会丢按键。
        /// </summary>
        public static void Reinstall()
        {
            KeyboardInterceptor interceptor = Program.Interceptor;
            if (interceptor == null)
            {
                return;
            }
            bool mouseWasOn = interceptor.MouseHookID != IntPtr.Zero;
            interceptor.DisableKeyboardHook();
            if (mouseWasOn) { interceptor.DisableMouseHook(); }
            interceptor.EnableKeyboardHook();
            if (mouseWasOn) { interceptor.EnableMouseHook(); }
            ReinstallCount++;
        }
    }
}
