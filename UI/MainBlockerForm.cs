using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI;
using KeyboardChatterBlocker.UI.Controls;
using KeyboardChatterBlocker.UI.Localization;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// 用户界面主窗体。
    /// <para>
    /// 本文件由原版 <c>MainBlockerForm.cs</c> 改写而来，<b>只动 UI</b>：
    /// 所有与 <see cref="Program.Blocker"/> 的交互、保存时机、自动禁用判定等逻辑与原版逐条一致；
    /// 变化仅在于 ① TabControl → 侧边导航，② 控件类型换成自绘版本，③ 文案中文化，
    /// ④ 统计页刷新间隔由 1000ms 收紧到 150ms（见 <see cref="StatsRefreshIntervalMs"/>，
    /// 自动保存的 30 分钟节奏保持不变）。
    /// </para>
    /// </summary>
    public partial class MainBlockerForm : ModernForm
    {
        /// <summary>Whether the form is still loading.</summary>
        public bool Loading = true;

        /// <summary>Timer that automatically updates the stats view ocassionally, if it's visible.</summary>
        public Timer StatsUpdateTimer;

        /// <summary>Whether the form is currently hidden from view.</summary>
        public bool IsHidden => !Visible;

        /// <summary>当前显示的页面。</summary>
        private MainPage _currentPage = MainPage.Log;

        /// <summary>
        /// 表格排序用的键名比较器。
        /// 忽略大小写、固定用不变文化 —— <see cref="Program.NormalizeCulture"/> 已把当前文化锁成
        /// Invariant，用固定比较器可保证排序结果在任何机器上都一致。
        /// </summary>
        /// <summary>
        /// 统计页刷新间隔（毫秒）。
        /// <para>
        /// 原版是 1000ms，按下按键后数字要等一秒才跳，手感迟钝。
        /// 这里调快只是为了视觉跟手：<see cref="Program.Blocker"/> 的 <c>AnyKeyChange</c> 为 false 时
        /// 整个 tick 只是两次布尔判断，空转开销可忽略，实际重建表格的频率仍受「有按键发生」约束。
        /// </para>
        /// </summary>
        private const int StatsRefreshIntervalMs = 150;

        /// <summary>上次把 <c>SaveStatsTicker</c> 加一的时间，用于把自动保存计时与刷新频率解耦。</summary>
        private DateTime _lastStatsSaveTick = DateTime.UtcNow;

        /// <summary>
        /// 长按救援的轮询定时器。
        /// <para>
        /// 只在对钩子线程无害的前提下才可能要求高频：它平时是<b>停着</b>的，
        /// 仅在拦下按键后、到该键松开之间才运行（通常几十毫秒），
        /// 所以常态下不会给输入路径增加任何负担。
        /// </para>
        /// </summary>
        private Timer HoldRescueTimer;

        /// <summary>救援轮询的粒度。越小判定越准，代价是这段时间内的唤醒次数。</summary>
        private const int HoldRescueTickMs = 20;

        /// <summary>Shows the form fully and properly.</summary>
        public void ShowForm()
        {
            Visible = true;
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
        }

        /// <summary>Hides the form fully and properly.</summary>
        public void HideForm()
        {
            if (!Program.DisableTrayIcon)
            {
                TrayIcon.Visible = true;
            }
            Visible = false;
        }

        /// <summary>
        /// Init the form.
        /// </summary>
        public MainBlockerForm()
        {
            Program.MainForm = this;
            Program.Blocker = new KeyBlocker();
            Program.Interceptor = new KeyboardInterceptor(Program.Blocker);
            Program.Blocker.AutoEnableMouse();
            Application.AddMessageFilter(new HotKeys.Internal.MessageFilter());
            Process currentProcess = Process.GetCurrentProcess();
            Process[] priorProcesses = Process.GetProcesses().Where(p => p.ProcessName == currentProcess.ProcessName && p.Id != currentProcess.Id).ToArray();
            if (priorProcesses.Any())
            {
                DialogResult result = MessageBox.Show(string.Format(Strings.DuplicateBodyFormat, priorProcesses[0].ProcessName, priorProcesses[0].Id),
                    Strings.DuplicateTitle, MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes:
                        foreach (Process proc in priorProcesses)
                        {
                            if (!proc.HasExited)
                            {
                                try
                                {
                                    proc.Kill();
                                }
                                catch (Exception)
                                {
                                    // Ignore for now
                                }
                            }
                        }
                        if (priorProcesses.Any(p => !p.HasExited))
                        {
                            DialogResult secondary = MessageBox.Show(Strings.CannotCloseBody, Strings.CannotCloseCaption, MessageBoxButtons.YesNo);
                            if (secondary != DialogResult.Yes)
                            {
                                Close();
                                return;
                            }
                        }
                        break;
                    case DialogResult.No:
                        break;
                    default:
                        Close();
                        return;
                }
            }
            Program.Blocker.KeyBlockedEvent += LogKeyBlocked;
            InitializeComponent();
            // 键盘测试页需要每一个键盘事件，而不只是被拦下的那些。
            // 放在 InitializeComponent 之后订阅，确保 TestKeyboardMap 已经建好。
            Program.Interceptor.KeyEvent += OnInterceptorKeyEvent;
            titleBar.PanelToggle += () => SetKeyboardPanelVisible(!_keyboardPanelVisible);
            SetKeyboardPanelVisible(true);
            versionAboutLabel.Text = string.Format(Strings.AboutVersionFormat, Application.ProductVersion);
            EnableEdgeResize(this);
            Load += MainBlockerForm_Load;
            FormClosing += MainBlockerForm_FormClosing;
            // 必须在 Shown 之后再做一次：Load 触发时窗体还没显示，焦点尚未分配，
            // 那时 IsEditing 还是 false，什么都拦不到。
            Shown += (s, e) => BlurThresholdBox();
        }

        /// <summary>
        /// Method auto-called (by event) for when a key is blocked.
        /// </summary>
        /// <param name="e">The key blocked event details.</param>
        public void LogKeyBlocked(KeyBlockedEventArgs e)
        {
            if (ChatterLogGrid == null || ChatterLogGrid.IsDisposed) { return; }
            bool wasScrolledToBottom = ChatterLogGrid.RowCount == 0
                || ChatterLogGrid.FirstDisplayedScrollingRowIndex + ChatterLogGrid.DisplayedRowCount(true) >= ChatterLogGrid.RowCount;
            ChatterLogGrid.Rows.Add(
                DateTime.Now.ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture),
                KeyNames.Display(e.Key),
                e.Time,
                Strings.CellEdit);
            // 有按键被拦下 → 立刻启动长按救援轮询。该定时器平时是停着的，只在有待救援按键时才跑，
            // 因此不会给钩子线程增加常态负担。
            if (HoldRescueTimer != null && !HoldRescueTimer.Enabled && Program.Blocker.HasPendingRescue)
            {
                HoldRescueTimer.Start();
            }
            // 追加的行同样要落进用户选的排序里（未排序时该调用零开销）
            ChatterLogGrid.ReapplySort();
            if (wasScrolledToBottom && ChatterLogGrid.RowCount > 0)
            {
                ChatterLogGrid.FirstDisplayedScrollingRowIndex = ChatterLogGrid.RowCount - 1;
            }
        }

        // ============================================================
        // 导航
        // ============================================================

        /// <summary>切换到指定页面。</summary>
        public void NavigateTo(MainPage page)
        {
            Panel target = PageFor(page);
            if (target == null) { return; }
            foreach (Control c in contentHost.Controls)
            {
                c.Visible = false;
            }
            target.Visible = true;
            _currentPage = page;
            UpdateNavSelection();
            BlurThresholdBox();
            if (!Loading)
            {
                PushStatsToGrid();
                if (page == MainPage.KeyboardTest)
                {
                    // 把焦点从侧边栏的数值输入框移开，否则在这一页敲键盘会被它吃掉
                    TestKeyboardMap.Focus();
                    // 自动禁用时钩子被卸载，页面上要说明一下，免得看起来像坏了
                    TestHintLabel.Text = Program.Blocker.IsAutoDisabled
                        ? Strings.TestHint + Strings.TestHintAutoDisabled
                        : Strings.TestHint;
                }
            }
        }

        private Panel PageFor(MainPage page)
        {
            switch (page)
            {
                case MainPage.Stats: return statsPage;
                case MainPage.Keys: return keysPage;
                case MainPage.KeyboardTest: return keyboardTestPage;
                case MainPage.AutoDisable: return autoDisablePage;
                case MainPage.OtherSettings: return otherSettingsPage;
                case MainPage.About: return aboutPage;
                default: return logPage;
            }
        }

        private void UpdateNavSelection()
        {
            navLog.Selected = _currentPage == MainPage.Log;
            navStats.Selected = _currentPage == MainPage.Stats;
            navKeys.Selected = _currentPage == MainPage.Keys;
            navKeyboardTest.Selected = _currentPage == MainPage.KeyboardTest;
            navAutoDisable.Selected = _currentPage == MainPage.AutoDisable;
            navSettings.Selected = _currentPage == MainPage.OtherSettings;
            navAbout.Selected = _currentPage == MainPage.About;
        }

        // ============================================================
        // 右侧键盘读数边栏
        // ============================================================

        /// <summary>右侧键盘读数边栏当前是否展开。</summary>
        private bool _keyboardPanelVisible = true;

        /// <summary>
        /// 把焦点从侧边栏的数值输入框移开。
        /// <para>
        /// 它是界面上唯一常驻的文本输入，启动时会自动抢到焦点 —— 于是用户在这个
        /// 键盘工具里随便敲什么，字符都会跑进阈值框里。必须在启动后和每次换页时
        /// 主动把焦点让出来。
        /// </para>
        /// </summary>
        private void BlurThresholdBox()
        {
            if (ChatterThresholdBox == null || !ChatterThresholdBox.IsEditing)
            {
                return;
            }
            try
            {
                ActiveControl = null;
            }
            catch (Exception)
            {
                // 个别情况下不允许置空
            }
            if (ChatterThresholdBox.IsEditing)
            {
                // 兜底：把焦点交给窗体自身，总好过让它留在输入框里
                try { Focus(); } catch (Exception) { }
            }
        }

        /// <summary>展开/收起右侧键盘读数边栏。</summary>
        public void SetKeyboardPanelVisible(bool visible)
        {
            if (rightPanel == null)
            {
                return;
            }
            _keyboardPanelVisible = visible;
            rightPanel.Visible = visible;
            // 列宽必须一起改 —— 只设 Visible 的话那一列仍然占着位置
            bodyLayout.ColumnStyles[2].Width = visible ? Metrics.Px(Metrics.SidebarWidth) : 0;
            titleBar.PanelToggleActive = visible;
            PerformLayout();
        }

        // ============================================================
        // 键盘测试页
        // ============================================================

        /// <summary>最近一次按下的键。</summary>
        private Keys _testLastKey = Keys.None;
        /// <summary>与上一次「同键」按下的间隔，毫秒；负值表示本次会话内该键还没有过上一次。</summary>
        private long _testSameKeyMs = -1;
        /// <summary>与上一次「任意键」按下的间隔，毫秒；负值表示还没有过上一次。</summary>
        private long _testSinceMs = -1;
        /// <summary>最近一次按下是否被放行。</summary>
        private bool _testLastAllowed = true;
        /// <summary>本次会话是否已有过按键。</summary>
        private bool _testHasReading;

        private readonly HashSet<Keys> _testDownKeys = new HashSet<Keys>();
        private readonly Dictionary<Keys, ulong> _testLastPressOfKey = new Dictionary<Keys, ulong>();
        private ulong _testLastPressAny;

        /// <summary>
        /// 收到任意键盘事件。
        /// <para>
        /// ⚠ 这个方法跑在钩子线程（= UI 线程）上、且处在输入路径中，必须尽快返回。
        /// 这里只做数值计算与设置控件文本 —— 设置 Text 只是标脏、不会立刻同步重绘，
        /// 所以不会把重绘开销压到输入延迟上。
        /// </para>
        /// </summary>
        private void OnInterceptorKeyEvent(Keys key, bool isDown, bool allowed)
        {
            // 站在键盘测试页时，按键更要优先给键盘图
            if (_currentPage == MainPage.KeyboardTest && ChatterThresholdBox.IsEditing)
            {
                TestKeyboardMap.Focus();
            }
            if (isDown)
            {
                ulong now = KeyBlocker.GetTickCount64();
                _testSameKeyMs = _testLastPressOfKey.TryGetValue(key, out ulong last) ? (long)(now - last) : -1;
                _testSinceMs = _testLastPressAny == 0 ? -1 : (long)(now - _testLastPressAny);
                _testLastPressOfKey[key] = now;
                _testLastPressAny = now;
                _testLastKey = key;
                _testLastAllowed = allowed;
                _testHasReading = true;
                _testDownKeys.Add(key);
                // 读数在右侧常驻边栏里，所以无论当前在哪一页都要刷新
                RenderTestStatus();
            }
            else
            {
                _testDownKeys.Remove(key);
                RenderTestDownKeys();
            }
            TestKeyboardMap.SetKeyState(key, isDown, allowed);
        }

        /// <summary>把状态区刷新成当前记录的读数。</summary>
        private void RenderTestStatus()
        {
            if (!_testHasReading)
            {
                TestLastKeyLabel.Text = Strings.TestNoKey;
                TestSinceLabel.Text = Strings.TestAwaiting;
                TestSameKeyLabel.Text = string.Empty;
                TestVerdictLabel.Visible = false;
            }
            else
            {
                TestLastKeyLabel.Text = KeyNames.Display(_testLastKey);
                TestSinceLabel.Text = FormatInterval(_testSinceMs);
                TestSameKeyLabel.Text = FormatInterval(_testSameKeyMs);
                TestVerdictLabel.Text = _testLastAllowed ? Strings.TestVerdictAllow : Strings.TestVerdictBlocked;
                TestVerdictLabel.ForeColor = _testLastAllowed ? ThemeManager.Current.Accent : ThemeManager.Current.DangerText;
                TestVerdictLabel.BackColor = _testLastAllowed
                    ? Color.FromArgb(48, ThemeManager.Current.Accent)
                    : Color.FromArgb(52, ThemeManager.Current.Danger);
                TestVerdictLabel.Visible = true;
            }
            RenderTestDownKeys();
        }

        private static string FormatInterval(long ms)
        {
            return ms < 0 ? Strings.TestNoKey : ms.ToString(CultureInfo.InvariantCulture) + " ms";
        }

        /// <summary>刷新「当前按住」那一行。</summary>
        private void RenderTestDownKeys()
        {
            if (_testDownKeys.Count == 0)
            {
                TestDownKeysLabel.Text = Strings.TestNoKey;
                return;
            }
            // 按按下顺序不保证，这里按键名排序，显示才稳定
            List<string> names = new List<string>();
            foreach (Keys k in _testDownKeys)
            {
                names.Add(KeyNames.Display(k));
            }
            names.Sort(StringComparer.InvariantCulture);
            TestDownKeysLabel.Text = string.Join(" + ", names);
        }

        // ============================================================
        // 常驻区事件
        // ============================================================

        /// <summary>
        /// Event method auto-called when the "Enable" check box is touched.
        /// </summary>
        public void EnabledCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.IsEnabled = EnabledCheckbox.Checked;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Event method auto-called when the "Chatter Threshold" box is touched.
        /// </summary>
        public void ChatterThresholdBox_ValueChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.GlobalChatterTimeLimit = (uint)ChatterThresholdBox.Value;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Event method auto-called when the tray icon is touched.
        /// </summary>
        public void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowForm();
        }

        /// <summary>
        /// Event method auto-called when the "Start In Tray" box is touched.
        /// </summary>
        public void TrayIconCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.HideInSystemTray = TrayIconCheckbox.Checked;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Location of the Windows startup folder (within the APPDATA environment variable).
        /// </summary>
        public const string STARTUP_FOLDER = "/Microsoft/Windows/Start Menu/Programs/Startup/";

        public static string StartupLinkPath => Environment.GetEnvironmentVariable("appdata") + STARTUP_FOLDER + "KeyboardChatterBlocker.lnk";

        /// <summary>
        /// Event method auto-called when the "Start With Windows" box is touched.
        /// </summary>
        public void StartWithWindowsCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            if (StartWithWindowsCheckbox.Checked)
            {
                try
                {
                    string folder = Environment.GetEnvironmentVariable("appdata") + STARTUP_FOLDER;
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    CreateStartupShortcut(StartupLinkPath);
                }
                catch (Exception ex)
                {
                    // 原版此处会抛出未捕获异常直接崩溃；UI 层改为提示后回滚勾选状态。
                    MessageBox.Show(string.Format(Strings.ErrorShortcutBody, ex.Message), Strings.ErrorShortcutTitle, MessageBoxButtons.OK);
                    Loading = true;
                    StartWithWindowsCheckbox.Checked = false;
                    Loading = false;
                }
            }
            else
            {
                if (File.Exists(StartupLinkPath))
                {
                    File.Delete(StartupLinkPath);
                }
            }
        }

        /// <summary>
        /// 创建开机自启快捷方式。
        /// 原版引用 <c>IWshRuntimeLibrary</c> COM 程序集（.NET Framework 专属），
        /// 这里改用迟绑定的 <c>WScript.Shell</c>，产出的 .lnk 路径/目标/工作目录/描述完全一致。
        /// </summary>
        private static void CreateStartupShortcut(string linkPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("系统中不可用 WScript.Shell");
            }
            object shell = Activator.CreateInstance(shellType);
            try
            {
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });
                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "开机自启 " + Strings.AppName });
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { Application.ExecutablePath });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(Application.ExecutablePath) });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                if (shortcut is MarshalByRefObject) { System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut); }
            }
            finally
            {
                if (shell is MarshalByRefObject) { System.Runtime.InteropServices.Marshal.ReleaseComObject(shell); }
            }
        }

        // ============================================================
        // 自动禁用
        // ============================================================

        public void SetAutoDisable(bool disable, string reason)
        {
            if (Program.Blocker.IsAutoDisabled == disable)
            {
                if (disable)
                {
                    string disableText = $"{Strings.AutoDisablePrefix}{reason}{Strings.AutoDisableSuffix}";
                    if (EnableNoteLabel.Text != disableText) // Redundant check to discourage redraw
                    {
                        EnableNoteLabel.Text = disableText;
                    }
                }
                return;
            }
            Program.Blocker.IsAutoDisabled = disable;
            if (disable)
            {
                Program.Blocker.Interceptor.DisableKeyboardHook();
                Program.Blocker.Interceptor.DisableMouseHook();
                // 钩子已卸载，收不到抬起事件了 —— 清掉键盘测试页的高亮，
                // 否则会留下「某个键一直按着」的假象。
                _testDownKeys.Clear();
                TestKeyboardMap.ClearAll();
                EnableNoteLabel.Text = $"{Strings.AutoDisablePrefix}{reason}{Strings.AutoDisableSuffix}";
                EnableNoteLabel.ForeColor = ThemeManager.Current.DangerText;
                EnableNoteLabel.BackColor = Color.FromArgb(52, ThemeManager.Current.Danger);
                EnableNoteLabel.Visible = true;
            }
            else
            {
                Program.Blocker.Interceptor.EnableKeyboardHook();
                Program.Blocker.AutoEnableMouse();
                EnableNoteLabel.Text = "";
                EnableNoteLabel.BackColor = Color.Transparent;
                EnableNoteLabel.Visible = false;
                foreach (string notBlocking in Program.Blocker.AutoDisablePrograms)
                {
                    SetAutoDisableProgramHighlight(notBlocking, false);
                }
            }
        }

        public void SetAutoDisableProgramHighlight(string program, bool isBlocking)
        {
            foreach (ListViewItem item in AutoDisableProgramsList.Items)
            {
                if (item.Text == program)
                {
                    item.BackColor = isBlocking ? Color.FromArgb(255, 128, 128) : Color.Transparent;
                }
            }
        }

        /// <summary>
        /// Check whether to auto-disable, and apply the correct value.
        /// </summary>
        public void CheckAutoDisable()
        {
            if (Program.Blocker.AutoDisableOnFullscreen)
            {
                if (FullScreenDetectHelper.IsFullscreen())
                {
                    SetAutoDisable(true, Strings.ReasonFullscreen);
                    foreach (ListViewItem item in AutoDisableProgramsList.Items)
                    {
                        item.BackColor = Color.Transparent;
                    }
                    return;
                }
            }
            HashSet<string> programsToCheck = new HashSet<string>(Program.Blocker.AutoDisablePrograms);
            if (programsToCheck.Count == 0)
            {
                SetAutoDisable(false, "none");
                return;
            }
            // 「仅前台」模式下候选集合只有前台窗口所属的那一个进程 ——
            // 比枚举全部进程更贴合意图，也便宜得多（不必每 2 秒给每个进程开一次句柄）。
            IEnumerable<string> candidates = Program.Blocker.AutoDisableForegroundOnly
                ? (GetForegroundProcessName() is string fg ? new[] { fg } : Enumerable.Empty<string>())
                : Process.GetProcesses().Select(p => p.ProcessName.ToLowerInvariant());
            bool any = false;
            foreach (string proc in candidates)
            {
                if (programsToCheck.Contains(proc))
                {
                    SetAutoDisableProgramHighlight(proc, true);
                    programsToCheck.Remove(proc);
                    SetAutoDisable(true, string.Format(Strings.ReasonProcessFormat, proc));
                    any = true;
                }
            }
            if (!any)
            {
                SetAutoDisable(false, "none");
            }
            else
            {
                foreach (string notBlocking in programsToCheck)
                {
                    SetAutoDisableProgramHighlight(notBlocking, false);
                }
            }
        }

        /// <summary>
        /// 取当前前台窗口所属进程的进程名（小写）；取不到返回 null。
        /// </summary>
        private static string GetForegroundProcessName()
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0)
            {
                return null;
            }
            try
            {
                using (Process p = Process.GetProcessById((int)pid))
                {
                    return p.ProcessName.ToLowerInvariant();
                }
            }
            catch (Exception)
            {
                // 进程可能刚好退出，或权限不足 —— 当作「没有前台命中」处理
                return null;
            }
        }

        // ============================================================
        // 生命周期
        // ============================================================

        /// <summary>
        /// Event method auto-called when the form loads.
        /// </summary>
        public void MainBlockerForm_Load(object sender, EventArgs e)
        {
            EnableNoteLabel.Text = "";
            EnableNoteLabel.BackColor = Color.Transparent;
            EnableNoteLabel.Visible = false;
            TrayIconCheckbox.Checked = Program.HideInSystemTray;
            if (Program.HideInSystemTray)
            {
                if (!Program.DisableTrayIcon)
                {
                    TrayIcon.Visible = true;
                }
                Timer hideProperlyTimer = new Timer() { Interval = 100 };
                hideProperlyTimer.Tick += (tickSender, tickArgs) =>
                {
                    WindowState = FormWindowState.Normal;
                    ShowInTaskbar = true;
                    Visible = false;
                    hideProperlyTimer.Stop();
                };
                hideProperlyTimer.Start();
            }
            Timer autoDisableTimer = new Timer() { Interval = 2000 };
            autoDisableTimer.Tick += (tickSender, tickArgs) => CheckAutoDisable();
            autoDisableTimer.Start();
            AutoDisableProgramsList.Items.AddRange(Program.Blocker.AutoDisablePrograms.Select(s => new ListViewItem(s)).ToArray());
            UpdateAutoDisableEmptyHint();
            AutoDisableOnFullscreenCheckbox.Checked = Program.Blocker.AutoDisableOnFullscreen;
            AutoDisableForegroundOnlyCheckbox.Checked = Program.Blocker.AutoDisableForegroundOnly;
            ChatterThresholdBox.Value = Program.Blocker.GlobalChatterTimeLimit;
            MeasureFromComboBox.SelectedIndex = Program.Blocker.MeasureMode == KeyBlocker.MeasureFrom.Release ? 1 : 0;
            EnabledCheckbox.Checked = Program.Blocker.IsEnabled;
            SaveStatsCheckbox.Checked = Program.Blocker.SaveStats;
            StartWithWindowsCheckbox.Checked = File.Exists(StartupLinkPath);
            OtherKeyResetsCheckbox.Checked = Program.Blocker.OtherKeyResetsTimeout;
            ExcludeInjectedCheckbox.Checked = Program.Blocker.ExcludeInjected;
            StatsUpdateTimer = new Timer { Interval = StatsRefreshIntervalMs };
            StatsUpdateTimer.Tick += StatsUpdateTimer_Tick;
            StatsUpdateTimer.Start();
            HoldRescueTimer = new Timer { Interval = HoldRescueTickMs };
            HoldRescueTimer.Tick += (tickSender, tickArgs) =>
            {
                Program.Blocker.ProcessHoldRescue();
                if (!Program.Blocker.HasPendingRescue)
                {
                    HoldRescueTimer.Stop();
                }
            };
            HoldRescueBox.Value = Program.Blocker.HoldRescueTime;
            PushKeysToGrid();
            Loading = false;
            NavigateTo(MainPage.Log);
            BlurThresholdBox();
            CheckAutoDisable();
        }

        /// <summary>
        /// Properly toggles whether the program is enabled.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            EnabledCheckbox.Checked = enabled;
        }

        /// <summary>
        /// Automatic stats update timer, when the stats view is visible.
        /// </summary>
        public void StatsUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (Program.Blocker.AnyKeyChange && !IsHidden && _currentPage == MainPage.Stats)
            {
                Program.Blocker.AnyKeyChange = false;
                PushStatsToGrid();
            }
            // 自动保存的节奏必须保持原版语义（每满 1 秒记一次，超过 60*30 秒后保存），
            // 否则调快刷新间隔会顺带把 30 分钟的自动保存变成几分钟，那就是行为变更了。
            if (Program.Blocker.SaveStats)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - _lastStatsSaveTick).TotalSeconds >= 1.0)
                {
                    _lastStatsSaveTick = now;
                    Program.Blocker.SaveStatsTicker++;
                    if (Program.Blocker.SaveStatsTicker > 60 * 30)
                    {
                        Program.Blocker.SaveStatsToFile(false);
                    }
                }
            }
        }

        /// <summary>
        /// If enabled, any close should fully close the form and exit the program.
        /// </summary>
        public bool ShouldForceClose = false;

        /// <summary>
        /// Event method auto-called when the form close button is pressed.
        /// </summary>
        public void MainBlockerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Blocker.SaveConfig(e.CloseReason == CloseReason.UserClosing);
            if (ShouldForceClose)
            {
                return;
            }
            if (e.CloseReason != CloseReason.UserClosing) // Don't block windows shutdown, etc.
            {
                return;
            }
            if (IsHidden) // If already hidden, any close must be an actual full close, so close.
            {
                return;
            }
            if (Program.HideInSystemTray)
            {
                e.Cancel = true;
                HideForm();
            }
        }

        // ============================================================
        // 表格推送
        // ============================================================

        /// <summary>
        /// Pushes all stats to the GUI grid.
        /// </summary>
        public void PushStatsToGrid()
        {
            StatsGrid.SuspendLayout();
            StatsGrid.Rows.Clear();
            foreach (KeyValuePair<Keys, int> keyData in Program.Blocker.StatsKeyCount.MainDictionary)
            {
                int chatterTotal = Program.Blocker.StatsKeyChatter[keyData.Key];
                string percentage = chatterTotal == 0 ? "" : ((chatterTotal * 100.0f / keyData.Value).ToString("00.00", CultureInfo.InvariantCulture) + "%");
                StatsGrid.Rows.Add(KeyNames.Display(keyData.Key), keyData.Value, chatterTotal, percentage);
            }
            StatsGrid.ResumeLayout(true);
            // 行序沿用原版（Dictionary 遍历顺序）；仅在用户点过列头时才重排
            StatsGrid.ReapplySort();
        }

        /// <summary>
        /// Pushes all keys to the GUI grid.
        /// </summary>
        public void PushKeysToGrid()
        {
            ConfigureKeysGrid.SuspendLayout();
            ConfigureKeysGrid.Rows.Clear();
            foreach (KeyValuePair<Keys, uint?> keyData in Program.Blocker.KeysToChatterTime.MainDictionary)
            {
                if (!keyData.Value.HasValue)
                {
                    continue;
                }
                ConfigureKeysGrid.Rows.Add(KeyNames.Display(keyData.Key), keyData.Value.Value, Strings.RemoveKey);
            }
            ConfigureKeysGrid.ResumeLayout(true);
            // 同上：行序沿用原版，仅在用户点过列头时重排
            ConfigureKeysGrid.ReapplySort();
        }

        // ============================================================
        // 按键配置页
        // ============================================================

        /// <summary>
        /// Event method auto-called when the "configure keys" grid is double-clicked.
        /// </summary>
        public void ConfigureKeysGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            if (!KeyNames.TryParseDisplay(ConfigureKeysGrid[0, e.RowIndex].Value?.ToString(), out Keys key))
            {
                MessageBox.Show(Strings.ErrorGridMisconfigured, Strings.ErrorTitle, MessageBoxButtons.OK);
                return;
            }
            if (e.ColumnIndex == 1) // Value column
            {
                uint resultValue = Program.Blocker.KeysToChatterTime[key] ?? Program.Blocker.GlobalChatterTimeLimit;
                using (KeyConfigurationForm keyConfigForm = new KeyConfigurationForm())
                {
                    keyConfigForm.Key = key;
                    keyConfigForm.SetResult = (i) => { resultValue = i; };
                    keyConfigForm.ShowDialog(this);
                }
                Program.Blocker.KeysToChatterTime[key] = resultValue;
                Program.Blocker.SaveConfig();
                ConfigureKeysGrid[1, e.RowIndex].Value = resultValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (e.ColumnIndex == 2) // Remove column
            {
                Program.Blocker.KeysToChatterTime[key] = null;
                Program.Blocker.SaveConfig();
                ConfigureKeysGrid.Rows.RemoveAt(e.RowIndex);
            }
        }

        /// <summary>
        /// Event method auto-called when the "configure keys" grid has a key pressed.
        /// </summary>
        private void ConfigureKeysGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (ConfigureKeysGrid.SelectedCells.Count != 1)
            {
                return;
            }
            int row = ConfigureKeysGrid.SelectedCells[0].RowIndex;
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                ConfigureKeysGrid_CellContentDoubleClick(null, new DataGridViewCellEventArgs(1, row));
            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                ConfigureKeysGrid_CellContentDoubleClick(null, new DataGridViewCellEventArgs(2, row));
            }
        }

        /// <summary>
        /// Event method auto-called when the "Add Key" button is pressed.
        /// </summary>
        public void AddKeyButton_Click(object sender, EventArgs e)
        {
            Keys? result = null;
            using (NeedInputForm form = new NeedInputForm())
            {
                form.SetResultKey = (k) => { result = k; };
                form.ShowDialog(this);
            }
            if (!result.HasValue)
            {
                return;
            }
            Program.Blocker.KeysToChatterTime[result.Value] = Program.Blocker.GlobalChatterTimeLimit;
            Program.Blocker.SaveConfig();
            // 重建而非追加，否则新键会落在末尾、破坏排序
            PushKeysToGrid();
        }

        // ============================================================
        // 抖动日志页
        // ============================================================

        /// <summary>
        /// Event method auto-called when the website link in the "About" tab is pressed.
        /// </summary>
        private void AboutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Strings.AboutUrl) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // 打不开浏览器就算了，不影响功能
            }
        }

        /// <summary>
        /// Event method auto-called when the "chatter log" grid is double-clicked.
        /// </summary>
        private void ChatterLogGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            if (!KeyNames.TryParseDisplay(ChatterLogGrid[1, e.RowIndex].Value?.ToString(), out Keys key))
            {
                MessageBox.Show(Strings.ErrorGridMisconfigured, Strings.ErrorTitle, MessageBoxButtons.OK);
                return;
            }
            if (e.ColumnIndex == 3) // 'Configure' column
            {
                if (!Program.Blocker.KeysToChatterTime[key].HasValue)
                {
                    Program.Blocker.KeysToChatterTime[key] = Program.Blocker.GlobalChatterTimeLimit;
                    Program.Blocker.SaveConfig();
                    // 同上：重建以保证有序
                    PushKeysToGrid();
                }
                string keyText = KeyNames.Display(key);
                NavigateTo(MainPage.Keys);
                foreach (DataGridViewRow row in ConfigureKeysGrid.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == keyText)
                    {
                        ConfigureKeysGrid.Select();
                        ConfigureKeysGrid.ClearSelection();
                        row.Selected = true;
                        ConfigureKeysGrid.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
        }

        // ============================================================
        // 自动禁用程序页
        // ============================================================

        /// <summary>
        /// Event method to keep a placeholder in the 'add program' text box.
        /// </summary>
        private void AddProgramTextBox_Enter(object sender, EventArgs e)
        {
            if (AddProgramTextBox.Text == Strings.ProgramPlaceholder)
            {
                AddProgramTextBox.Text = "";
                AddProgramTextBox.ForeColor = ThemeManager.Current.Text;
            }
        }

        /// <summary>
        /// Event method to keep a placeholder in the 'add program' text box.
        /// </summary>
        private void AddProgramTextBox_Leave(object sender, EventArgs e)
        {
            if (AddProgramTextBox.Text == "")
            {
                AddProgramTextBox.Text = Strings.ProgramPlaceholder;
                AddProgramTextBox.ForeColor = ThemeManager.Current.TextMuted;
            }
        }

        /// <summary>
        /// A few common processes that are always running in Windows and thus don't need to be listed (for convenience).
        /// </summary>
        public static HashSet<string> StandardWindowsProcesses = new HashSet<string>()
        {
            "svchost", "smartscreen", "spoolsv", "explorer", "services", "registry", "taskhost", "taskhostw", "smss", "ctfmon", "idle", "csrss", "dwm", "fontdrvhost", "lsass", "sgrmbroker",
            "onedrive", "searchhost", "searchindexer", "shellexperiencehost", "startmenuexperiencehost", "system", "systemsettings", "systemsettingsbroker", "wininit", "winlogin", "winlogon", "wmiprvse",
            "applicationframehost", "textinputhost", "aggregatorhost", "audiodg", "gamebar", "gamebarftserver", "gamingservices", "gamingservicesnet", "microsoft.photos",
            "minisearchhost", "memory compression", "msmpeng", "msmpengcp", "nissrv", "runtimebroker", "sihost", "uhssvc", "unsecapp", "wudfhost", "yourphone",
            "rtkauduservice64", // realtek audio
            "nvcontainer", "nvidia share", "nvsphelper64", "nvbroadcast.container", "nvidia broadcast", "nvidia broadcast ui", "nvdisplay.container", "nvidia web helper", // nvidia
             "jhi_service", "lms", // intel
        };

        /// <summary>
        /// Event method to show a list of current program names for the add program box.
        /// </summary>
        private void ShowProgramListButton_Click(object sender, EventArgs e)
        {
            // Get a hashset of exclusions by combining the list of already-disabled programs with the set of common windows programs
            HashSet<string> excludeProcessNames = StandardWindowsProcesses.Union(Program.Blocker.AutoDisablePrograms).ToHashSet();
            // Get a list of processes, and make them unique by process name - prioritize processes with a window title over those without
            List<Process> processes = Process.GetProcesses().GroupBy(p => p.ProcessName).Select(g => g.FirstOrDefault(p => !string.IsNullOrEmpty(p.MainWindowTitle)) ?? g.First())
                // then exclude the set of excludes
                .Where(p => !excludeProcessNames.Contains(p.ProcessName.ToLowerInvariant()))
                // Sort the list alphabetically, but pull those with main window titles to the top (and those without below)
                .OrderBy(p => p.ProcessName).OrderBy(p => string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
            // 原版用已废弃的 ContextMenu/MenuItem，这里换成 ContextMenuStrip（行为一致，且能跟随暗色主题）
            ContextMenuStrip menu = new ContextMenuStrip { ShowImageMargin = false };
            foreach (Process p in processes)
            {
                string label = string.IsNullOrEmpty(p.MainWindowTitle) ? p.ProcessName : $"{p.ProcessName} ({p.MainWindowTitle})";
                string name = p.ProcessName.ToLowerInvariant();
                menu.Items.Add(new ToolStripMenuItem(label, null, (s, a) =>
                {
                    AddProgramTextBox.ForeColor = ThemeManager.Current.Text;
                    AddProgramTextBox.Text = name;
                }));
            }
            if (menu.Items.Count == 0)
            {
                menu.Items.Add(new ToolStripMenuItem("（没有可添加的进程）") { Enabled = false });
            }
            menu.Show(ShowProgramListButton, new Point(0, ShowProgramListButton.Height));
        }

        /// <summary>
        /// Event method to add an auto-disabled program.
        /// </summary>
        private void AddToListButton_Click(object sender, EventArgs e)
        {
            if (AddProgramTextBox.Text.Trim().Length == 0)
            {
                return;
            }
            string program = AddProgramTextBox.Text.ToLowerInvariant().Trim();
            if (Program.Blocker.AutoDisablePrograms.Contains(program))
            {
                return;
            }
            Program.Blocker.AutoDisablePrograms.Add(program);
            Program.Blocker.SaveConfig();
            AutoDisableProgramsList.Items.Add(new ListViewItem(program));
            UpdateAutoDisableEmptyHint();
            AddProgramTextBox.Text = "";
            AddProgramTextBox_TextChanged(null, null);
        }

        /// <summary>
        /// Event method to set whether the add-program button is clickable.
        /// </summary>
        private void AddProgramTextBox_TextChanged(object sender, EventArgs e)
        {
            string text = AddProgramTextBox.Text.Trim();
            bool eitherEnabled = text.Length != 0 && AddProgramTextBox.Text != Strings.ProgramPlaceholder;
            bool isExistingProgram = Program.Blocker.AutoDisablePrograms.Contains(text.ToLowerInvariant());
            AddToListButton.Enabled = eitherEnabled && !isExistingProgram;
            RemoveProgramButton.Enabled = eitherEnabled && isExistingProgram;
        }

        /// <summary>
        /// Event method to remove an auto-disabled program.
        /// </summary>
        private void RemoveProgramButton_Click(object sender, EventArgs e)
        {
            if (AddProgramTextBox.Text.Trim().Length == 0)
            {
                return;
            }
            string program = AddProgramTextBox.Text.ToLowerInvariant().Trim();
            if (!Program.Blocker.AutoDisablePrograms.Contains(program))
            {
                return;
            }
            Program.Blocker.AutoDisablePrograms.Remove(program);
            Program.Blocker.SaveConfig();
            foreach (ListViewItem item in AutoDisableProgramsList.Items)
            {
                if (item.Text == program)
                {
                    AutoDisableProgramsList.Items.Remove(item);
                    break;
                }
            }
            UpdateAutoDisableEmptyHint();
            AddProgramTextBox.Text = "";
            AddProgramTextBox_TextChanged(null, null);
        }

        /// <summary>
        /// Event method to auto-alter the current program text box input to be an item selected from the list.
        /// </summary>
        private void AutoDisableProgramsList_Click(object sender, EventArgs e)
        {
            ListViewItem selected = AutoDisableProgramsList.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
            if (selected != null)
            {
                AddProgramTextBox.ForeColor = ThemeManager.Current.Text;
                AddProgramTextBox.Text = selected.Text;
            }
        }

        private void UpdateAutoDisableEmptyHint()
        {
            if (autoDisableEmptyHint != null)
            {
                autoDisableEmptyHint.Visible = AutoDisableProgramsList.Items.Count == 0;
            }
        }

        // ============================================================
        // 其他设置页
        // ============================================================

        /// <summary>
        /// Event method to handle the 'auto disable on fullscreen' checkbox state changing.
        /// </summary>
        private void AutoDisableOnFullscreenCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.AutoDisableOnFullscreen = AutoDisableOnFullscreenCheckbox.Checked;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Event method to handle the 'foreground only' checkbox state changing.
        /// 立即重新判定一次，免得要等 2 秒的轮询才生效。
        /// </summary>
        private void AutoDisableForegroundOnlyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.AutoDisableForegroundOnly = AutoDisableForegroundOnlyCheckbox.Checked;
            Program.Blocker.SaveConfig();
            CheckAutoDisable();
        }

        /// <summary>
        /// Event method to handle the 'other key reset' checkbox state changing.
        /// </summary>
        private void OtherKeyReset_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.OtherKeyResetsTimeout = OtherKeyResetsCheckbox.Checked;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Event method to handle the 'measure from' dropdown state changing.
        /// 原版用 Enum.TryParse(下拉框文本)，中文化后会让枚举解析静默失效，
        /// 故改为按 SelectedIndex 判定（0 = Press，1 = Release）。
        /// config 里的 measure_from 序列化格式不受影响。
        /// </summary>
        private void MeasureFromComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.MeasureMode = MeasureFromComboBox.SelectedIndex == 1
                ? KeyBlocker.MeasureFrom.Release
                : KeyBlocker.MeasureFrom.Press;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Event method to handle the 'save stats' checkbox state changing.
        /// </summary>
        private void SaveStatsCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.SaveStats = SaveStatsCheckbox.Checked;
        }

        /// <summary>
        /// Event method to handle the 'exclude injected' checkbox state changing.
        /// </summary>
        private void ExcludeInjectedCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.ExcludeInjected = ExcludeInjectedCheckbox.Checked;
            Program.Blocker.SaveConfig();
        }

        /// <summary>
        /// Event method to handle the 'hold rescue' threshold box changing.
        /// 0 = 关闭该功能。
        /// </summary>
        private void HoldRescueBox_ValueChanged(object sender, EventArgs e)
        {
            if (Loading)
            {
                return;
            }
            Program.Blocker.HoldRescueTime = (uint)HoldRescueBox.Value;
            Program.Blocker.SaveConfig();
        }
    }
}
