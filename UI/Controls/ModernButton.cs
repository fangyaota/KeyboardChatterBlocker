using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>按钮视觉样式。</summary>
    public enum ButtonVariant
    {
        /// <summary>次要按钮：描边 + 透明底（默认）。</summary>
        Secondary,
        /// <summary>主要按钮：强调色填充。</summary>
        Primary,
        /// <summary>危险按钮：红色描边。</summary>
        Danger,
        /// <summary>纯文字按钮：无边框无底色。</summary>
        Ghost
    }

    /// <summary>
    /// 自绘圆角按钮。
    /// 继承 <see cref="Button"/> 以便原 UI 代码里的 .Text / .Enabled / .Click 全部继续可用。
    /// </summary>
    public class ModernButton : Button
    {
        private bool _hover;
        private bool _pressed;
        private bool _focusable = true;

        /// <summary>
        /// 是否接受焦点，默认 <c>true</c>。
        /// <para>
        /// 设为 <c>false</c> 后按钮<b>只能用鼠标点</b>：Tab 跳不过去，空格/回车也不会触发它。
        /// 「键盘测试」页的「重置」需要这个 —— 那一页的每一次按键都应该被当成测试数据，
        /// 不能有控件把空格/回车吃掉。
        /// </para>
        /// <para>
        /// 光设 <c>TabStop = false</c> 是不够的，那只能挡住 Tab 键。完整做法是三件事一起做：
        /// 关掉 <see cref="ControlStyles.Selectable"/>（挡 Tab / SelectNextControl）、
        /// 在 <see cref="OnGotFocus"/> 里把焦点让出去（挡鼠标点击）、
        /// 在 <see cref="OnKeyDown"/> 里吞掉空格与回车（最后一道硬保证）。
        /// </para>
        public bool Focusable
        {
            get { return _focusable; }
            set
            {
                if (_focusable == value) { return; }
                _focusable = value;
                SetStyle(ControlStyles.Selectable, value);
                TabStop = value;
                if (!value && Focused)
                {
                    // 已经握着焦点的话得交出去，否则它仍然会响应空格/回车
                    if (Parent != null) { Parent.SelectNextControl(this, true, true, true, true); }
                }
                Invalidate();
            }
        }

        /// <summary>防止 OnGotFocus 里把焦点让出去时又绕回自己。</summary>
        private bool _yieldingFocus;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 建句柄时 WinForms 会把 Selectable 重新打开，必须在那之后再关一次
            if (!_focusable) { SetStyle(ControlStyles.Selectable, false); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Font = Fonts.Body;
            Height = Metrics.ButtonHeight;
            Cursor = Cursors.Hand;
            Variant = ButtonVariant.Secondary;
        }

        /// <summary>视觉样式。</summary>
        public ButtonVariant Variant { get; set; }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { _pressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { _pressed = false; Invalidate(); base.OnMouseUp(mevent); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        /// <summary>
        /// 不可获焦点时，一旦拿到焦点就立刻让给下一个控件。
        /// <para>
        /// 光关 <see cref="ControlStyles.Selectable"/> 挡不住焦点：
        /// <see cref="ButtonBase"/> 在鼠标按下时会绕过 <c>Selectable</c> 直接调 <c>Focus()</c>，
        /// 所以点一下照样会拿到焦点。这里在焦点落下的瞬间把它交出去。
        /// </para>
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (_focusable || _yieldingFocus) { return; }
            _yieldingFocus = true;
            try
            {
                Control c = Parent;
                // 跳过自己往下找（TabStop=false，不会被选中）；实在没有别的控件就让容器接住
                if (c == null || !c.SelectNextControl(this, true, true, true, true)) { c?.Focus(); }
            }
            finally { _yieldingFocus = false; }
        }

        /// <summary>
        /// 不可获焦点时，空格/回车一律不响应。
        /// 这是「按空格不会误触按钮」的硬保证 —— 不依赖焦点有没有被成功让出去。
        /// 按键本身仍会被全局钩子收到（测试页的键盘图和统计走的是钩子，不是窗口消息）。
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs kevent)
        {
            if (!_focusable && (kevent.KeyCode == Keys.Space || kevent.KeyCode == Keys.Enter))
            {
                kevent.Handled = true;
                kevent.SuppressKeyPress = true;
                return;
            }
            base.OnKeyDown(kevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Palette p = ThemeManager.Current;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // 先用父容器底色铺满，圆角外才不会残留上一帧

            int radius = Metrics.Px(Metrics.SmallRadius);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color fill, border, text;
            if (!Enabled)
            {
                fill = Variant == ButtonVariant.Primary ? Palette.Mix(p.Accent, p.CardBg, 0.6f) : Color.Transparent;
                border = Variant == ButtonVariant.Ghost ? Color.Transparent : p.CardBorder;
                text = p.TextDisabled;
            }
            else
            {
                switch (Variant)
                {
                    case ButtonVariant.Primary:
                        fill = _pressed ? p.AccentPressed : (_hover ? p.AccentHover : p.Accent);
                        border = Color.Transparent;
                        text = p.AccentText;
                        break;
                    case ButtonVariant.Danger:
                        fill = _pressed ? Drawing.Shade(p.DangerSoft, -0.10f) : (_hover ? Drawing.Shade(p.DangerSoft, 0.06f) : Color.Transparent);
                        border = p.Danger;
                        text = p.Danger;
                        break;
                    case ButtonVariant.Ghost:
                        fill = _pressed ? p.ControlPressed : (_hover ? p.ControlHover : Color.Transparent);
                        border = Color.Transparent;
                        text = p.Text;
                        break;
                    default:
                        fill = _pressed ? p.ControlPressed : (_hover ? p.ControlHover : p.ControlBg);
                        border = p.ControlBorder;
                        text = p.Text;
                        break;
                }
            }

            Drawing.FillRoundedRect(g, r, radius, fill, border, border.A > 0 ? Metrics.Px(1) : 0);
            Drawing.DrawText(g, Text, Font, text, ClientRectangle, ContentAlignment.MiddleCenter, clearType: true);
        }
    }
}
