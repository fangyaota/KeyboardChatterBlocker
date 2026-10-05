using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// Main entry point class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The Key Blocker instance.
        /// </summary>
        public static KeyBlocker Blocker;

        /// <summary>
        /// Whether the program should hide in the system tray.
        /// </summary>
        public static bool HideInSystemTray = false;

        /// <summary>
        /// Whether the tray icon should be disabled even when the program is "hidden in system tray".
        /// </summary>
        public static bool DisableTrayIcon = false;

        /// <summary>
        /// The main form, <see cref="MainBlockerForm"/>.
        /// </summary>
        public static MainBlockerForm MainForm;

        /// <summary>
        /// The interceptor instance.
        /// </summary>
        public static KeyboardInterceptor Interceptor;

        /// <summary>
        /// Forces the system to use standard Invariant culture to avoid bugs induced by Microsoft's broken auto-localization.
        /// See also https://github.com/FreneticLLC/FreneticUtilities/blob/master/FreneticUtilities/FreneticToolkit/SpecialTools.cs#L19
        /// </summary>
        public static void NormalizeCulture()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Force-close the process.
        /// </summary>
        public static void Close()
        {
            Program.MainForm.Close();
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            NormalizeCulture();
            // 键盘设备跟踪辅助进程：同一份 exe 的第二个实例，只注册 Raw Input 报设备，
            // 不建窗体、不装钩子、不进托盘。必须隔离成独立进程 —— 原因见 UI/KeyboardDevices.cs。
            if (DeviceTrackerHost.IsTrackerInvocation(args))
            {
                DeviceTrackerHost.Run(args);
                return;
            }
            // If triggered by the installer, close this and relaunch, to avoid hanging up the installer.
            if (args.Length == 1 && args[0] == "_INSTALLER_AUTOBOUNCE")
            {
                Process.Start(Application.ExecutablePath);
                return;
            }
            // This needs priority to prevent delaying input
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;

            // 以下几项必须在创建任何窗口之前设置。
            // 注意：MainBlockerForm 的构造函数会提前访问 Handle（HotKeys.Register 需要），
            // 因此不能把它们挪到窗体构造之后。
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetDefaultFont(Fonts.Body);
            ThemeManager.Initialize();
            // 必须在创建任何控件之前确定缩放系数（见 Metrics 中的说明）
            Metrics.Initialize();
            // .NET 9+ 让原生滚动条 / 右键菜单 / 下拉同步变暗的正解
            Application.SetColorMode(ThemeManager.IsDark ? SystemColorMode.Dark : SystemColorMode.Classic);

            MainForm = new MainBlockerForm();
            Application.Run(MainForm);
        }
    }
}
