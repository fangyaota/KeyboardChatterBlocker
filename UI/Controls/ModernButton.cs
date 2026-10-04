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
