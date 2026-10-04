using System;
using System.Runtime.InteropServices;

namespace KeyboardChatterBlocker.UI.Theme
{
    /// <summary>UI 层用到的 Win32 互操作声明。</summary>
    internal static class NativeMethods
    {
        // —— 窗口消息 ——
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_NCCALCSIZE = 0x0083;
        public const int WM_DPICHANGED = 0x02E0;

        public const int HTCAPTION = 2;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        /// <summary>类样式：在无边框窗口上启用系统投影。</summary>
        public const int CS_DROPSHADOW = 0x00020000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // —— 前台窗口（自动禁用程序的前台判定用）——
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // —— DWM（Win11 圆角 / 边框色）——
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_BORDER_COLOR = 34;
        public const int DWMWCP_DEFAULT = 0;
        public const int DWMWCP_DONOTROUND = 1;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;
        /// <summary>DWMWA_COLOR_NONE：让 DWM 不绘制边框（消除 Win11 的细白边）。</summary>
        public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        /// <summary>检测 Win11（内部版本号 &gt;= 22000），圆角仅在 Win11 生效。</summary>
        public static bool IsWindows11OrGreater()
        {
            Version v = Environment.OSVersion.Version;
            return v.Major >= 10 && v.Build >= 22000;
        }
    }
}
