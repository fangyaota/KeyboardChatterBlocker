using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 自绘数值输入框。
    /// 原生 <see cref="NumericUpDown"/> 的上下箭头由 <c>UpDownBase</c> 原生绘制、无法换肤
    /// （改内嵌 TextBox 的颜色只能变输入区，箭头永远是浅色），因此这里完全自绘：
    /// 左侧是一个无边框 <see cref="TextBox"/> 负责输入，右侧自绘加减按钮。
    /// <para>对外暴露 <c>Value</c>(decimal) / <c>Minimum</c> / <c>Maximum</c> / <c>Increment</c> / <c>ValueChanged</c>。</para>
    /// </summary>
    public class ModernNumericUpDown : Control
    {
        private readonly TextBox _edit;
        private decimal _value;
        private decimal _minimum;
        private decimal _maximum = 1000;
        private decimal _increment = 10;
        private int _hoverButton;   // 0 = 无，1 = 上，-1 = 下
        private bool _hover;

        private const int ButtonWidth = 22;

        public ModernNumericUpDown()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Font = Fonts.Body;
            Height = Metrics.Px(Metrics.InputHeight);
            BackColor = ThemeManager.Current.ControlBg;

            _edit = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Fonts.Body,
                TextAlign = HorizontalAlignment.Left,
                BackColor = ThemeManager.Current.ControlBg,
                ForeColor = ThemeManager.Current.Text,
            };
            _edit.KeyDown += Edit_KeyDown;
            _edit.LostFocus += (s, e) => CommitEdit();
            _edit.GotFocus += (s, e) => { _edit.SelectAll(); Invalidate(); };
            Controls.Add(_edit);
            LayoutEdit();
            SyncEditText();
        }

        /// <summary>当前值。赋值会自动钳制到 [Minimum, Maximum] 并触发 <see cref="ValueChanged"/>。</summary>
        public decimal Value
        {
            get { return _value; }
            set
            {
                decimal clamped = Clamp(value);
                if (clamped == _value) { SyncEditText(); return; }
                _value = clamped;
                SyncEditText();
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>最小值。</summary>
        public decimal Minimum { get { return _minimum; } set { _minimum = value; Value = _value; } }

        /// <summary>最大值。</summary>
        public decimal Maximum { get { return _maximum; } set { _maximum = value; Value = _value; } }

        /// <summary>步进值。</summary>
        public decimal Increment { get { return _increment; } set { if (value > 0) { _increment = value; } } }

        /// <summary>数值变化事件。</summary>
        public event EventHandler ValueChanged;

        private decimal Clamp(decimal v)
        {
            if (v < _minimum) { return _minimum; }
            if (v > _maximum) { return _maximum; }
            return v;
        }

        private void SyncEditText()
        {
            string text = _value.ToString(CultureInfo.InvariantCulture);
            if (_edit.Text != text && !_edit.Focused) { _edit.Text = text; }
        }

        private void CommitEdit()
        {
            if (decimal.TryParse(_edit.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
            {
                Value = parsed;
            }
            SyncEditText();
            if (_edit.Text != _value.ToString(CultureInfo.InvariantCulture))
            {
                _edit.Text = _value.ToString(CultureInfo.InvariantCulture);
            }
            Invalidate();
        }

        private void Edit_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up: e.Handled = true; Value = _value + _increment; break;
                case Keys.Down: e.Handled = true; Value = _value - _increment; break;
                case Keys.PageUp: e.Handled = true; Value = _value + _increment * 10; break;
                case Keys.PageDown: e.Handled = true; Value = _value - _increment * 10; break;
                case Keys.Enter: e.Handled = true; CommitEdit(); break;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutEdit();
        }

        private void LayoutEdit()
        {
            // 构造函数里设置 Height 会触发 OnResize，此时 _edit 尚未创建
            if (_edit == null) { return; }
            int btnW = Metrics.Px(ButtonWidth);
            int pad = Metrics.Px(10);
            int h = Font.Height;
            _edit.SetBounds(pad, Math.Max(0, (Height - h) / 2), Math.Max(0, Width - btnW - pad - Metrics.Px(4)), h);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Focused) { return; }
            Value = e.Delta > 0 ? _value + _increment : _value - _increment;
            ((HandledMouseEventArgs)e).Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int btn = ButtonAt(e.Location);
            if (btn != _hoverButton || !_hover)
            {
                _hoverButton = btn;
                _hover = true;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverButton = 0;
            _hover = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int btn = ButtonAt(e.Location);
            if (btn == 1) { Value = _value + _increment; }
            else if (btn == -1) { Value = _value - _increment; }
            else { _edit.Focus(); }
            Invalidate();
        }

        private int ButtonAt(Point p)
        {
            int btnW = Metrics.Px(ButtonWidth);
            if (p.X < Width - btnW) { return 0; }
            return p.Y < Height / 2 ? 1 : -1;
        }

        protected override void OnGotFocus(EventArgs e) { _edit.Focus(); Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        /// <summary>按钮区域宽度（含），供布局参考。</summary>
        public int ButtonAreaWidth => Metrics.Px(ButtonWidth);

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_edit == null) { return; }
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int radius = Metrics.Px(Metrics.SmallRadius);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            bool active = Focused || _edit.Focused;
            Color border = active ? p.Accent : (_hover ? p.CardBorderStrong : p.ControlBorder);
            Drawing.FillRoundedRect(g, r, radius, p.ControlBg, border, Metrics.Px(1));

            int btnW = Metrics.Px(ButtonWidth);
            Rectangle btnArea = new Rectangle(Width - btnW, 1, btnW - 1, Height - 2);

            // 分隔线
            using (Pen sep = new Pen(p.ControlBorder, Metrics.Px(1)))
            {
                g.DrawLine(sep, btnArea.Left, btnArea.Top + Metrics.Px(3), btnArea.Left, btnArea.Bottom - Metrics.Px(3));
            }

            int half = btnArea.Height / 2;
            Rectangle upRect = new Rectangle(btnArea.Left, btnArea.Top, btnArea.Width, half);
            Rectangle downRect = new Rectangle(btnArea.Left, btnArea.Top + half, btnArea.Width, btnArea.Height - half);

            if (_hoverButton == 1) { FillButtonBg(g, upRect, p); }
            else if (_hoverButton == -1) { FillButtonBg(g, downRect, p); }

            Color arrowColor = Enabled ? p.TextMuted : p.TextDisabled;
            DrawArrow(g, upRect, true, arrowColor);
            DrawArrow(g, downRect, false, arrowColor);

            // 文本颜色跟随
            _edit.BackColor = p.ControlBg;
            _edit.ForeColor = Enabled ? p.Text : p.TextDisabled;
        }

        private void FillButtonBg(Graphics g, Rectangle rect, Palette p)
        {
            using (SolidBrush b = new SolidBrush(p.ControlHover)) { g.FillRectangle(b, rect); }
        }

        private void DrawArrow(Graphics g, Rectangle rect, bool up, Color color)
        {
            using (Pen pen = new Pen(color, Metrics.Px(2)))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                float cx = rect.Left + rect.Width / 2f;
                float cy = rect.Top + rect.Height / 2f;
                float w = Metrics.Px(4);
                float h = Metrics.Pxf(2.5f);
                if (up)
                {
                    g.DrawLines(pen, new[] { new PointF(cx - w, cy + h * 0.6f), new PointF(cx, cy - h * 0.6f), new PointF(cx + w, cy + h * 0.6f) });
                }
                else
                {
                    g.DrawLines(pen, new[] { new PointF(cx - w, cy - h * 0.6f), new PointF(cx, cy + h * 0.6f), new PointF(cx + w, cy - h * 0.6f) });
                }
            }
        }
    }
}
