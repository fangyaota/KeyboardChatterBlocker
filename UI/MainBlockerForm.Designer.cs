using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI;
using KeyboardChatterBlocker.UI.Controls;
using KeyboardChatterBlocker.UI.Localization;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker
{
    /// <summary>主界面上的页面。</summary>
    public enum MainPage
    {
        Log, Stats, Keys, AutoDisable, OtherSettings, About
    }

    partial class MainBlockerForm
    {
        // —— 窗口外壳 ——
        private TitleBar titleBar;
        private TableLayoutPanel bodyLayout;
        private Panel sidebarPanel;
        private TableLayoutPanel sidebarLayout;
        private CardPanel statusCard;
        private CardPanel startupCard;
        private Panel navPanel;
        private Panel contentHost;

        // —— 页面宿主 ——
        private Panel logPage;
        private Panel statsPage;
        private Panel keysPage;
        private Panel autoDisablePage;
        private Panel otherSettingsPage;
        private Panel aboutPage;

        // —— 状态卡片 ——
        public ToggleSwitch EnabledCheckbox;
        public ModernNumericUpDown ChatterThresholdBox;
        public ModernLabel EnableNoteLabel;
        private ModernLabel statusCaption;
        private ModernLabel thresholdCaption;
        private ModernLabel msLabel;

        // —— 启动卡片 ——
        public ModernCheckBox StartWithWindowsCheckbox;
        public ModernCheckBox TrayIconCheckbox;

        // —— 导航 ——
        private SideNavButton navLog;
        private SideNavButton navStats;
        private SideNavButton navKeys;
        private SideNavButton navAutoDisable;
        private SideNavButton navSettings;
        private SideNavButton navAbout;

        // —— 抖动日志页 ——
        public ModernDataGridView ChatterLogGrid;
        private DataGridViewTextBoxColumn colLogTime;
        private DataGridViewTextBoxColumn colLogKey;
        private DataGridViewTextBoxColumn colLogDelay;
        private DataGridViewTextBoxColumn colLogConfigure;

        // —— 统计页 ——
        public ModernDataGridView StatsGrid;
        private DataGridViewTextBoxColumn colStatsKey;
        private DataGridViewTextBoxColumn colStatsCount;
        private DataGridViewTextBoxColumn colStatsChatter;
        private DataGridViewTextBoxColumn colStatsRate;

        // —— 按键配置页 ——
        public ModernDataGridView ConfigureKeysGrid;
        public ModernButton AddKeyButton;
        private DataGridViewTextBoxColumn colCfgKey;
        private DataGridViewTextBoxColumn colCfgThreshold;
        private DataGridViewTextBoxColumn colCfgRemove;

        // —— 自动禁用程序页 ——
        public ModernListView AutoDisableProgramsList;
        public ModernCheckBox AutoDisableOnFullscreenCheckbox;
        public ModernTextBox AddProgramTextBox;
        public ModernButton AddToListButton;
        public ModernButton RemoveProgramButton;
        public ModernButton ShowProgramListButton;
        private FieldShell addProgramShell;
        private ModernLabel autoDisableEmptyHint;

        // —— 其他设置页 ——
        public ModernComboBox MeasureFromComboBox;
        public ModernCheckBox ExcludeInjectedCheckbox;
        public ModernCheckBox SaveStatsCheckbox;
        public ModernCheckBox OtherKeyResetsCheckbox;

        // —— 关于页 ——
        public LinkLabel AboutLinkLabel;
        public ModernLabel versionAboutLabel;
        private PictureBox aboutIconBox;

        // —— 托盘 ——
        public NotifyIcon TrayIcon;
        public ContextMenuStrip trayIconContextMenu;
        public ToolStripMenuItem ContextMenuShowButton;
        public ToolStripMenuItem ContextMenuExitButton;

        // —— 96 DPI 基准的逻辑尺寸（全部经 P() 换算成设备像素）——
        private const int SidebarCellMargin = 12;
        private const int StatusCardHeight = 168;
        private const int StartupCardHeight = 112;
        private const int PageMargin = 12;

        /// <summary>逻辑像素 → 设备像素的简写。</summary>
        private static int P(int logical) => Metrics.Px(logical);

        /// <summary>四边相同的逻辑边距 → 设备边距。</summary>
        private static Padding Pad(int all) => new Padding(P(all));

        private void InitializeComponent()
        {
            SuspendLayout();

            // ============ 窗口 ============
            Text = Strings.AppNameFull;
            Icon = AppIcons.App;
            Size = new Size(P(920), P(620));
            MinimumSize = new Size(P(820), P(560));
            BackColor = ThemeManager.Current.WindowBg;
            ShowInTaskbar = true;

            // ============ 托盘 ============
            ContextMenuShowButton = new ToolStripMenuItem(Strings.TrayShow);
            ContextMenuExitButton = new ToolStripMenuItem(Strings.TrayExit);
            trayIconContextMenu = new ContextMenuStrip();
            trayIconContextMenu.Items.AddRange(new ToolStripItem[] { ContextMenuShowButton, new ToolStripSeparator(), ContextMenuExitButton });
            TrayIcon = new NotifyIcon
            {
                Icon = AppIcons.Small(),
                Text = Strings.AppNameFull,
                ContextMenuStrip = trayIconContextMenu,
                Visible = false,
            };
            TrayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;
            ContextMenuShowButton.Click += (s, e) => ShowForm();
            ContextMenuExitButton.Click += (s, e) => { ShouldForceClose = true; Close(); };

            // ============ 标题栏 ============
            titleBar = new TitleBar
            {
                TitleText = Strings.AppName,
                SubtitleText = Strings.AppSubtitle,
                BarIcon = AppIcons.Small(),
            };

            // ============ 主体：左导航 + 右内容 ============
            bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ThemeManager.Current.WindowBg,
                           CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };;
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(Metrics.SidebarWidth)));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            sidebarPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = ThemeManager.Current.SidebarBg };
            contentHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = ThemeManager.Current.WindowBg };

            BuildSidebar();
            BuildPages();

            sidebarPanel.Controls.Add(sidebarLayout);
            bodyLayout.Controls.Add(sidebarPanel, 0, 0);
            bodyLayout.Controls.Add(contentHost, 1, 0);

            Controls.Add(bodyLayout);
            Controls.Add(titleBar);

            ResumeLayout(true);
        }

        // ============================================================
        // 左侧栏
        // ============================================================
        private void BuildSidebar()
        {
            sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ThemeManager.Current.SidebarBg,
                           CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };;
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, P(StatusCardHeight)));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, P(StartupCardHeight)));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            BuildStatusCard();
            BuildStartupCard();
            BuildNav();

            sidebarLayout.Controls.Add(statusCard, 0, 0);
            sidebarLayout.Controls.Add(startupCard, 0, 1);
            sidebarLayout.Controls.Add(navPanel, 0, 2);
        }

        /// <summary>卡片内可用的宽度（设备像素）。</summary>
        private static int CardInnerWidth =>
            P(Metrics.SidebarWidth) - P(SidebarCellMargin) * 2 - P(Metrics.CardPadding) * 2;

        private void BuildStatusCard()
        {
            int pad = P(Metrics.CardPadding);
            int inner = CardInnerWidth;

            statusCard = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(P(SidebarCellMargin), P(SidebarCellMargin), P(SidebarCellMargin), 0),
            };

            statusCaption = MakeCaption("屏蔽状态");
            statusCaption.SetBounds(pad, pad - P(4), inner, P(18));

            EnabledCheckbox = new ToggleSwitch
            {
                Text = Strings.Enable,
                Width = inner,
                Height = P(24),
            };
            EnabledCheckbox.SetBounds(pad, pad + P(16), inner, P(24));
            EnabledCheckbox.CheckedChanged += EnabledCheckbox_CheckedChanged;

            EnableNoteLabel = new ModernLabel
            {
                Pill = true,
                Font = Fonts.Small,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = ThemeManager.Current.CardBg,
                Text = string.Empty,
                Visible = false,
            };
            EnableNoteLabel.SetBounds(pad, pad + P(48), inner, P(24));

            thresholdCaption = MakeCaption(Strings.GlobalThreshold);
            thresholdCaption.SetBounds(pad, pad + P(78), inner, P(18));

            ChatterThresholdBox = new ModernNumericUpDown
            {
                Minimum = 0,
                Maximum = 1000,
                Increment = 10,
                Value = 100,
            };
            ChatterThresholdBox.SetBounds(pad, pad + P(98), P(108), P(32));
            ChatterThresholdBox.ValueChanged += ChatterThresholdBox_ValueChanged;

            msLabel = MakeCaption(Strings.Milliseconds);
            msLabel.SetBounds(pad + P(114), pad + P(98), inner - P(114), P(32));

            statusCard.Controls.Add(EnableNoteLabel);
            statusCard.Controls.Add(msLabel);
            statusCard.Controls.Add(ChatterThresholdBox);
            statusCard.Controls.Add(thresholdCaption);
            statusCard.Controls.Add(EnabledCheckbox);
            statusCard.Controls.Add(statusCaption);
        }

        private void BuildStartupCard()
        {
            int pad = P(Metrics.CardPadding);
            int inner = CardInnerWidth;

            startupCard = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(P(SidebarCellMargin), P(SidebarCellMargin), P(SidebarCellMargin), 0),
            };

            ModernLabel caption = MakeCaption("启动");
            caption.SetBounds(pad, pad - P(4), inner, P(18));

            StartWithWindowsCheckbox = new ModernCheckBox
            {
                Text = Strings.StartWithWindows,
                Width = inner,
                Height = P(22),
            };
            StartWithWindowsCheckbox.SetBounds(pad, pad + P(18), inner, P(22));
            StartWithWindowsCheckbox.CheckedChanged += StartWithWindowsCheckbox_CheckedChanged;

            TrayIconCheckbox = new ModernCheckBox
            {
                Text = Strings.StartInTray,
                Width = inner,
                Height = P(22),
            };
            TrayIconCheckbox.SetBounds(pad, pad + P(46), inner, P(22));
            TrayIconCheckbox.CheckedChanged += TrayIconCheckbox_CheckedChanged;

            startupCard.Controls.Add(TrayIconCheckbox);
            startupCard.Controls.Add(StartWithWindowsCheckbox);
            startupCard.Controls.Add(caption);
        }

        private void BuildNav()
        {
            navPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Current.SidebarBg };

            navLog = MakeNav(NavGlyph.Log, Strings.NavLog, 0);
            navStats = MakeNav(NavGlyph.Stats, Strings.NavStats, 1);
            navKeys = MakeNav(NavGlyph.Keys, Strings.NavKeys, 2);
            navAutoDisable = MakeNav(NavGlyph.AutoDisable, Strings.NavAutoDisable, 3);
            navSettings = MakeNav(NavGlyph.Settings, Strings.NavSettings, 4);
            navAbout = MakeNav(NavGlyph.About, Strings.NavAbout, 5);

            navLog.Click += (s, e) => NavigateTo(MainPage.Log);
            navStats.Click += (s, e) => NavigateTo(MainPage.Stats);
            navKeys.Click += (s, e) => NavigateTo(MainPage.Keys);
            navAutoDisable.Click += (s, e) => NavigateTo(MainPage.AutoDisable);
            navSettings.Click += (s, e) => NavigateTo(MainPage.OtherSettings);
            navAbout.Click += (s, e) => NavigateTo(MainPage.About);
        }

        private SideNavButton MakeNav(NavGlyph glyph, string text, int index)
        {
            SideNavButton button = new SideNavButton(glyph, text);
            button.SetBounds(P(8), index * P(Metrics.NavItemHeight) + P(4), P(Metrics.SidebarWidth) - P(16), P(Metrics.NavItemHeight));
            navPanel.Controls.Add(button);
            return button;
        }

        // ============================================================
        // 页面
        // ============================================================
        private void BuildPages()
        {
            logPage = BuildLogPage();
            statsPage = BuildStatsPage();
            keysPage = BuildKeysPage();
            autoDisablePage = BuildAutoDisablePage();
            otherSettingsPage = BuildOtherSettingsPage();
            aboutPage = BuildAboutPage();

            contentHost.Controls.Add(aboutPage);
            contentHost.Controls.Add(otherSettingsPage);
            contentHost.Controls.Add(autoDisablePage);
            contentHost.Controls.Add(keysPage);
            contentHost.Controls.Add(statsPage);
            contentHost.Controls.Add(logPage);
        }

        private Panel BuildLogPage()
        {
            Panel page = MakePage();

            ModernDataGridView grid = new ModernDataGridView { Dock = DockStyle.Fill };
            colLogTime = new DataGridViewTextBoxColumn { HeaderText = Strings.ColTime, Width = P(150) };
            // 「按键」列吃掉剩余宽度，避免右侧留一条突兀的空白
            colLogKey = new DataGridViewTextBoxColumn { HeaderText = Strings.ColKey, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = P(140) };
            colLogDelay = new DataGridViewTextBoxColumn { HeaderText = Strings.ColChatterDelay, Width = P(180), ValueType = typeof(int) };
            colLogConfigure = new DataGridViewTextBoxColumn { HeaderText = Strings.ColConfigure, Width = P(90) };
            grid.Columns.AddRange(colLogTime, colLogKey, colLogDelay, colLogConfigure);
            grid.CellContentDoubleClick += ChatterLogGrid_CellContentDoubleClick;
            ChatterLogGrid = grid;

            page.Controls.Add(MakeCard(Strings.LogTitle, Strings.LogHint, grid));
            return page;
        }

        private Panel BuildStatsPage()
        {
            Panel page = MakePage();

            ModernDataGridView grid = new ModernDataGridView { Dock = DockStyle.Fill };
            colStatsKey = new DataGridViewTextBoxColumn { HeaderText = Strings.ColKey, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = P(160) };
            colStatsCount = new DataGridViewTextBoxColumn { HeaderText = Strings.ColCount, Width = P(120), ValueType = typeof(int) };
            colStatsChatter = new DataGridViewTextBoxColumn { HeaderText = Strings.ColChatter, Width = P(120), ValueType = typeof(int) };
            colStatsRate = new DataGridViewTextBoxColumn { HeaderText = Strings.ColRate, Width = P(110) };
            grid.Columns.AddRange(colStatsKey, colStatsCount, colStatsChatter, colStatsRate);
            StatsGrid = grid;

            page.Controls.Add(MakeCard(Strings.StatsTitle, Strings.StatsHint, grid));
            return page;
        }

        private Panel BuildKeysPage()
        {
            Panel page = MakePage();

            ModernDataGridView grid = new ModernDataGridView { Dock = DockStyle.Fill };
            colCfgKey = new DataGridViewTextBoxColumn { HeaderText = Strings.ColKey, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = P(180) };
            colCfgThreshold = new DataGridViewTextBoxColumn { HeaderText = Strings.ColThreshold, Width = P(170), ValueType = typeof(int) };
            colCfgRemove = new DataGridViewTextBoxColumn { HeaderText = Strings.ColRemove, Width = P(100) };
            grid.Columns.AddRange(colCfgKey, colCfgThreshold, colCfgRemove);
            grid.CellContentDoubleClick += ConfigureKeysGrid_CellContentDoubleClick;
            grid.KeyDown += ConfigureKeysGrid_KeyDown;
            ConfigureKeysGrid = grid;

            CardPanel card = MakeCard(Strings.KeysTitle, Strings.KeysHint, grid);

            AddKeyButton = new ModernButton
            {
                Text = Strings.AddKey,
                Variant = ButtonVariant.Primary,
                Size = new Size(P(120), P(Metrics.ButtonHeight)),
            };
            AddKeyButton.Click += AddKeyButton_Click;
            card.Controls.Add(AddKeyButton);
            AddKeyButton.BringToFront();
            card.Resize += (s, e) => PositionOverlay(AddKeyButton, card);

            page.Controls.Add(card);
            return page;
        }

        private Panel BuildAutoDisablePage()
        {
            Panel page = MakePage();
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };

            ModernLabel title = new ModernLabel
            {
                Text = Strings.AutoDisableTitle,
                Font = Fonts.Heading,
                Dock = DockStyle.Top,
                Height = P(34),
            };
            ModernLabel hint = new ModernLabel
            {
                Text = Strings.AutoDisableHint,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Dock = DockStyle.Top,
                Height = P(22),
            };

            autoDisableEmptyHint = new ModernLabel
            {
                Text = Strings.AutoDisableEmpty,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Dock = DockStyle.Top,
                Height = P(22),
            };

            AutoDisableOnFullscreenCheckbox = new ModernCheckBox
            {
                Text = Strings.AutoDisableFullscreen,
                Dock = DockStyle.Top,
                Height = P(26),
            };
            AutoDisableOnFullscreenCheckbox.CheckedChanged += AutoDisableOnFullscreenCheckbox_CheckedChanged;

            TableLayoutPanel addRow = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                ColumnCount = 4,
                RowCount = 1,
                Height = P(40),
                BackColor = ThemeManager.Current.CardBg,
            };
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(110)));
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(110)));
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(130)));
            addRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 原版在 Designer 里把输入框初始文本设为占位符，这里保持一致
            AddProgramTextBox = new ModernTextBox
            {
                Dock = DockStyle.Fill,
                PreserveForeColor = true,
                Text = Strings.ProgramPlaceholder,
                ForeColor = ThemeManager.Current.TextMuted,
            };
            AddProgramTextBox.TextChanged += AddProgramTextBox_TextChanged;
            AddProgramTextBox.Enter += AddProgramTextBox_Enter;
            AddProgramTextBox.Leave += AddProgramTextBox_Leave;

            addProgramShell = new FieldShell { Dock = DockStyle.Fill, Margin = new Padding(0, P(4), P(8), P(4)) };
            addProgramShell.Controls.Add(AddProgramTextBox);

            AddToListButton = new ModernButton
            {
                Text = Strings.AddToList,
                Variant = ButtonVariant.Primary,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, P(4), P(8), P(4)),
                Enabled = false,
            };
            AddToListButton.Click += AddToListButton_Click;

            RemoveProgramButton = new ModernButton
            {
                Text = Strings.RemoveFromList,
                Variant = ButtonVariant.Danger,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, P(4), P(8), P(4)),
                Enabled = false,
            };
            RemoveProgramButton.Click += RemoveProgramButton_Click;

            ShowProgramListButton = new ModernButton
            {
                Text = Strings.ShowProgramList,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, P(4), 0, P(4)),
            };
            ShowProgramListButton.Click += ShowProgramListButton_Click;

            addRow.Controls.Add(addProgramShell, 0, 0);
            addRow.Controls.Add(AddToListButton, 1, 0);
            addRow.Controls.Add(RemoveProgramButton, 2, 0);
            addRow.Controls.Add(ShowProgramListButton, 3, 0);

            AutoDisableProgramsList = new ModernListView { Dock = DockStyle.Fill };
            AutoDisableProgramsList.Click += AutoDisableProgramsList_Click;

            // 添加顺序：Fill 最先（最后布局），Top/Bottom 依次向上叠
            card.Controls.Add(AutoDisableProgramsList);
            card.Controls.Add(addRow);
            card.Controls.Add(autoDisableEmptyHint);
            card.Controls.Add(AutoDisableOnFullscreenCheckbox);
            card.Controls.Add(hint);
            card.Controls.Add(title);

            page.Controls.Add(card);
            return page;
        }

        private Panel BuildOtherSettingsPage()
        {
            Panel page = MakePage();
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };

            TableLayoutPanel stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = ThemeManager.Current.CardBg,
                           CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };;
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, P(34)));   // 标题
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, P(24)));   // 提示
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, P(80)));   // 计时起点
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, P(112)));  // 复选框组
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            ModernLabel title = new ModernLabel { Text = Strings.OtherTitle, Font = Fonts.Heading, Dock = DockStyle.Fill };
            ModernLabel hint = new ModernLabel
            {
                Text = Strings.OtherHint,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Dock = DockStyle.Fill,
            };

            // —— 计时起点 ——
            TableLayoutPanel measureRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ThemeManager.Current.CardBg,
                           CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };;
            measureRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(90)));
            measureRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            measureRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            ModernLabel measureCaption = new ModernLabel
            {
                Text = Strings.MeasureFrom,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Panel measureRight = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Current.CardBg };
            MeasureFromComboBox = new ModernComboBox { Size = new Size(P(180), P(Metrics.InputHeight)) };
            MeasureFromComboBox.Items.AddRange(new object[] { Strings.MeasurePress, Strings.MeasureRelease });
            MeasureFromComboBox.SelectedIndex = 0;
            MeasureFromComboBox.Location = new Point(0, P(22));
            MeasureFromComboBox.SelectedIndexChanged += MeasureFromComboBox_SelectedIndexChanged;

            ModernLabel measureHint = new ModernLabel
            {
                Text = Strings.MeasureHint,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(P(196), P(26)),
                Height = P(22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            measureRight.Controls.Add(measureHint);
            measureRight.Controls.Add(MeasureFromComboBox);
            measureRight.Resize += (s, e) => measureHint.Width = System.Math.Max(P(120), measureRight.Width - P(202));
            measureRow.Controls.Add(measureCaption, 0, 0);
            measureRow.Controls.Add(measureRight, 1, 0);

            // —— 复选框组 ——
            // 用「直接堆叠」而非 TableLayoutPanel：后者会给每个单元格画 1px 灰色分隔线，
            // 在深色主题下尤其突兀。
            Panel checks = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = ThemeManager.Current.CardBg,
            };

            ExcludeInjectedCheckbox = new ModernCheckBox
            {
                Text = Strings.ExcludeInjected,
                Dock = DockStyle.Top,
                Margin = Padding.Empty,
                Height = P(36),
            };
            ExcludeInjectedCheckbox.CheckedChanged += ExcludeInjectedCheckbox_CheckedChanged;

            SaveStatsCheckbox = new ModernCheckBox
            {
                Text = Strings.SaveStats,
                Dock = DockStyle.Top,
                Margin = Padding.Empty,
                Height = P(36),
            };
            SaveStatsCheckbox.CheckedChanged += SaveStatsCheckbox_CheckedChanged;

            OtherKeyResetsCheckbox = new ModernCheckBox
            {
                Text = Strings.OtherKeysReset,
                Dock = DockStyle.Top,
                Margin = Padding.Empty,
                Height = P(36),
            };
            OtherKeyResetsCheckbox.CheckedChanged += OtherKeyReset_CheckedChanged;

            // Dock=Top 的叠放顺序与添加顺序相反，故倒序添加
            checks.Controls.Add(OtherKeyResetsCheckbox);
            checks.Controls.Add(SaveStatsCheckbox);
            checks.Controls.Add(ExcludeInjectedCheckbox);

            stack.Controls.Add(title, 0, 0);
            stack.Controls.Add(hint, 0, 1);
            stack.Controls.Add(measureRow, 0, 2);
            stack.Controls.Add(checks, 0, 3);
            card.Controls.Add(stack);

            page.Controls.Add(card);
            return page;
        }

        private Panel BuildAboutPage()
        {
            Panel page = MakePage();
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            int pad = P(Metrics.CardPadding);

            aboutIconBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = AppIcons.App?.ToBitmap(),
                Location = new Point(pad, pad + P(6)),
                Size = new Size(P(48), P(48)),
                BackColor = ThemeManager.Current.CardBg,
            };

            ModernLabel name = new ModernLabel
            {
                Text = Strings.AppName,
                Font = Fonts.Heading,
                Location = new Point(pad + P(64), pad + P(4)),
                Size = new Size(P(420), P(28)),
            };
            ModernLabel subtitle = new ModernLabel
            {
                Text = Strings.AppSubtitle,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(pad + P(64), pad + P(32)),
                Size = new Size(P(420), P(20)),
            };
            ModernLabel portNote = new ModernLabel
            {
                Text = Strings.AboutPortNote,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(pad, pad + P(76)),
                Size = new Size(P(560), P(22)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            ModernLabel author = new ModernLabel
            {
                Text = Strings.AboutAuthor,
                Location = new Point(pad, pad + P(114)),
                Size = new Size(P(560), P(22)),
            };
            ModernLabel copyright = new ModernLabel
            {
                Text = Strings.AboutCopyright,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(pad, pad + P(138)),
                Size = new Size(P(560), P(22)),
            };
            ModernLabel license = new ModernLabel
            {
                Text = Strings.AboutLicense,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(pad, pad + P(162)),
                Size = new Size(P(560), P(22)),
            };

            AboutLinkLabel = new LinkLabel
            {
                Text = Strings.AboutLinkText,
                Font = Fonts.Body,
                LinkColor = ThemeManager.Current.Accent,
                ActiveLinkColor = ThemeManager.Current.AccentHover,
                VisitedLinkColor = ThemeManager.Current.Accent,
                Location = new Point(pad, pad + P(194)),
                Size = new Size(P(560), P(22)),
                BackColor = ThemeManager.Current.CardBg,
            };
            AboutLinkLabel.LinkClicked += AboutLinkLabel_LinkClicked;

            versionAboutLabel = new ModernLabel
            {
                Text = string.Empty,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(pad, pad + P(226)),
                Size = new Size(P(560), P(22)),
            };

            card.Controls.Add(versionAboutLabel);
            card.Controls.Add(AboutLinkLabel);
            card.Controls.Add(license);
            card.Controls.Add(copyright);
            card.Controls.Add(author);
            card.Controls.Add(portNote);
            card.Controls.Add(subtitle);
            card.Controls.Add(name);
            card.Controls.Add(aboutIconBox);

            page.Controls.Add(card);
            return page;
        }

        // ============================================================
        // 小工具
        // ============================================================

        /// <summary>创建一个页面容器。</summary>
        private static Panel MakePage()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Pad(PageMargin),
                BackColor = ThemeManager.Current.WindowBg,
                Visible = false,
            };
        }

        /// <summary>创建一张「标题 + 说明 + 内容」的标准卡片。</summary>
        private static CardPanel MakeCard(string title, string hint, Control content)
        {
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };

            TableLayoutPanel stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ThemeManager.Current.CardBg,
                           CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };;
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, P(34)));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, P(26)));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            ModernLabel titleLabel = new ModernLabel { Text = title, Font = Fonts.Heading, Dock = DockStyle.Fill };
            ModernLabel hintLabel = new ModernLabel
            {
                Text = hint,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Dock = DockStyle.Fill,
            };

            content.Dock = DockStyle.Fill;
            stack.Controls.Add(titleLabel, 0, 0);
            stack.Controls.Add(hintLabel, 0, 1);
            stack.Controls.Add(content, 0, 2);
            card.Controls.Add(stack);
            return card;
        }

        private static ModernLabel MakeCaption(string text)
        {
            return new ModernLabel
            {
                Text = text,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
            };
        }

        /// <summary>把浮层按钮摆到卡片右上角。</summary>
        private void PositionOverlay(Control overlay, CardPanel card)
        {
            int pad = P(Metrics.CardPadding);
            overlay.Location = new Point(card.Width - overlay.Width - pad, pad - P(2));
        }
    }
}
