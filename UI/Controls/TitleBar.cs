using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>标题栏上的窗口按钮类型。</summary>
    public enum CaptionButtonKind
    {
        Minimize,
        Maximize,
        /// <summary>已最大化时显示的「还原」图标。</summary>
        Restore,
        /// <summary>右侧键盘读数边栏的显示/隐藏开关。</summary>
        Panel,
        /// <summary>「强制最上层显示」的开关。</summary>
        Pin,
        Close
    }

    /// <summary>
    /// 自绘标题栏：图标 + 应用名 + 副标题 + 窗口按钮。
    /// </summary>
    public class TitleBar : Panel
    {
        private readonly CaptionButton _minButton;
        private readonly CaptionButton _maxButton;
        private readonly CaptionButton _closeButton;
        private readonly CaptionButton _panelButton;
        private readonly CaptionButton _pinButton;
        private readonly Panel _panelGap;
        private Icon _icon;

        public TitleBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = Metrics.Px(Metrics.TitleBarHeight);
            BackColor = ThemeManager.Current.WindowBg;

            _closeButton = new CaptionButton(CaptionButtonKind.Close) { Dock = DockStyle.Right, Width = Metrics.Px(46) };
            _maxButton = new CaptionButton(CaptionButtonKind.Maximize) { Dock = DockStyle.Right, Width = Metrics.Px(46) };
            _minButton = new CaptionButton(CaptionButtonKind.Minimize) { Dock = DockStyle.Right, Width = Metrics.Px(46) };
            // 右侧读数边栏的开关。用一段不绘制的间隔把它和三个窗口按钮分开，
            // 免得看起来像第四个窗口按钮。
            _panelButton = new CaptionButton(CaptionButtonKind.Panel) { Dock = DockStyle.Right, Width = Metrics.Px(42) };
            _pinButton = new CaptionButton(CaptionButtonKind.Pin) { Dock = DockStyle.Right, Width = Metrics.Px(42) };
            _panelGap = new Panel { Dock = DockStyle.Right, Width = Metrics.Px(16), BackColor = ThemeManager.Current.WindowBg };

            // WinForms 停靠规则：Controls 集合里「最后添加」的控件最先贴边，落在最右侧。
            // 所以「从左到右」的视觉顺序，正好等于「添加顺序」。
            // 期望： [置顶] [边栏开关] [间隔] [最小化] [最大化] [关闭]
            Controls.Add(_pinButton);
            Controls.Add(_panelButton);
            Controls.Add(_panelGap);
            Controls.Add(_minButton);
            Controls.Add(_maxButton);
            Controls.Add(_closeButton);

            _minButton.Click += (s, e) => FindForm()?.WindowState = FormWindowState.Minimized;
            _maxButton.Click += (s, e) => ToggleMaximize();
            _closeButton.Click += (s, e) => FindForm()?.Close();
            _panelButton.Click += (s, e) => PanelToggle?.Invoke();
            _pinButton.Click += (s, e) => PinToggle?.Invoke();

            MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { (FindForm() as ModernForm)?.DragWindow(); } };
            DoubleClick += (s, e) => ToggleMaximize();
        }

        /// <summary>主标题（中文应用名）。</summary>
        public string TitleText { get; set; } = "键盘防抖";

        /// <summary>副标题（英文原名）。</summary>
        public string SubtitleText { get; set; } = "Keyboard Chatter Blocker";

        /// <summary>标题栏左侧图标。</summary>
        public Icon BarIcon
        {
            get { return _icon; }
            set { _icon = value; Invalidate(); }
        }

        /// <summary>右侧键盘读数边栏的开关被点击。</summary>
        public event Action PanelToggle;

        /// <summary>边栏当前是否显示（按钮会跟着高亮）。</summary>
        public bool PanelToggleActive
        {
            get { return _panelButton.Active; }
            // 注意：必须让按钮自己重绘 —— 按钮是独立子控件，Invalidate 标题栏不会连带重绘它
            set { if (_panelButton.Active != value) { _panelButton.Active = value; _panelButton.Invalidate(); } }
        }

        /// <summary>「强制最上层显示」的开关被点击。</summary>
        public event Action PinToggle;

        /// <summary>当前是否置顶（按钮会跟着高亮）。</summary>
        public bool PinToggleActive
        {
            get { return _pinButton.Active; }
            set { if (_pinButton.Active != value) { _pinButton.Active = value; _pinButton.Invalidate(); } }
        }

        /// <summary>最大化/还原按钮，供外部同步状态。</summary>
        public void SyncMaximizeState(FormWindowState state)
        {
            _maxButton.Kind = state == FormWindowState.Maximized ? CaptionButtonKind.Restore : CaptionButtonKind.Maximize;
        }

        /// <summary>在最大化与还原之间切换。</summary>
        private void ToggleMaximize()
        {
            Form f = FindForm();
            if (f == null) { return; }
            f.WindowState = f.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            SyncMaximizeState(f.WindowState);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(p.WindowBg)) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int pad = Metrics.Px(14);
            int iconSize = Metrics.Px(18);
            int x = pad;

            if (_icon != null)
            {
                g.DrawIcon(_icon, new Rectangle(x, (Height - iconSize) / 2, iconSize, iconSize));
                x += iconSize + Metrics.Px(10);
            }

            Size titleSize = TextRenderer.MeasureText(TitleText, Fonts.Title);
            Rectangle titleRect = new Rectangle(x, 0, titleSize.Width + Metrics.Px(4), Height);
            Drawing.DrawText(g, TitleText, Fonts.Title, p.Text, titleRect, ContentAlignment.MiddleLeft, clearType: false);
            x += titleRect.Width + Metrics.Px(8);

            int rightLimit = _panelButton.Left - LogicalToDeviceUnits(6);
            Rectangle subRect = new Rectangle(x, 0, Math.Max(0, rightLimit - x - pad), Height);
            Drawing.DrawText(g, SubtitleText, Fonts.Small, p.TextMuted, subRect, ContentAlignment.MiddleLeft, clearType: false);

            // 底部分隔线
            using (Pen pen = new Pen(p.CardBorder, Metrics.Px(1)))
            {
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }
        }
    }

    /// <summary>标题栏的最小化 / 最大化 / 关闭按钮。</summary>
    internal class CaptionButton : Control
    {
        private bool _hover;
        private bool _pressed;

        public CaptionButton(CaptionButtonKind kind)
        {
            Kind = kind;
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public CaptionButtonKind Kind { get; set; }

        /// <summary>开关型按钮的「已开启」状态，会以强调色高亮。</summary>
        public bool Active { get; set; }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(p.WindowBg)) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            bool isClose = Kind == CaptionButtonKind.Close;
            if (Active && !_hover && !_pressed)
            {
                using (SolidBrush b = new SolidBrush(Drawing.ParentBackColor(this, p.WindowBg))) { g.FillRectangle(b, ClientRectangle); }
            }
            if (_hover || _pressed)
            {
                Color bg = isClose
                    ? (_pressed ? Drawing.Shade(p.Danger, -0.15f) : p.Danger)
                    : (_pressed ? p.ControlPressed : p.ControlHover);
                using (SolidBrush b = new SolidBrush(bg)) { g.FillRectangle(b, ClientRectangle); }
            }

            Color fg = (_hover && isClose) ? Color.White : (Active ? p.Accent : p.Text);
            int cx = Width / 2;
            int cy = Height / 2;
            int s = Metrics.Px(5);
            using (Pen pen = new Pen(fg, Metrics.Pxf(1.4f)))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                switch (Kind)
                {
                    case CaptionButtonKind.Close:
                        g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                        g.DrawLine(pen, cx + s, cy - s, cx - s, cy + s);
                        break;
                    case CaptionButtonKind.Minimize:
                        g.DrawLine(pen, cx - s, cy, cx + s, cy);
                        break;
                    case CaptionButtonKind.Maximize:
                        g.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                        break;
                    case CaptionButtonKind.Panel:
                    {
                        // 一个矩形，右侧一小条填充 —— 表示「右侧面板」
                        Rectangle body = new Rectangle(cx - s - LogicalToDeviceUnits(2), cy - s, s * 2 + LogicalToDeviceUnits(4), s * 2);
                        g.DrawRectangle(pen, body);
                        int strip = Math.Max(LogicalToDeviceUnits(2), body.Width / 3);
                        using (SolidBrush b = new SolidBrush(fg))
                        {
                            g.FillRectangle(b, new Rectangle(body.Right - strip - 1, body.Top + 1, strip, body.Height - 1));
                        }
                        break;
                    }
                    case CaptionButtonKind.Pin:
                    {
                        // 顶部一条横线 + 向上的箭头 = 「钉在最上层」
                        int topY = cy - s + Metrics.Px(2);
                        int head = Math.Max(2, s / 2);
                        g.DrawLine(pen, cx - s, cy - s, cx + s, cy - s);
                        g.DrawLine(pen, cx, topY, cx, cy + s);
                        g.DrawLine(pen, cx - head, topY + head, cx, topY);
                        g.DrawLine(pen, cx + head, topY + head, cx, topY);
                        break;
                    }
                    case CaptionButtonKind.Restore:
                        g.DrawRectangle(pen, cx - s, cy - s + Metrics.Px(2), s * 2 - Metrics.Px(2), s * 2 - Metrics.Px(2));
                        g.DrawLine(pen, cx - s + Metrics.Px(2), cy - s, cx + s, cy - s);
                        g.DrawLine(pen, cx + s, cy - s, cx + s, cy + s - Metrics.Px(2));
                        break;
                }
            }
        }
    }
}
