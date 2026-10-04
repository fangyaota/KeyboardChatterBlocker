using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 自绘复选框（圆角方块 + 勾）。
    /// 继承 <see cref="CheckBox"/>，.Checked / .CheckedChanged / .Text 全部照旧可用。
    /// </summary>
    public class ModernCheckBox : CheckBox
    {
        private bool _hover;

        public ModernCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Font = Fonts.Body;
            AutoSize = false;
            Height = Metrics.Px(22);
            Cursor = Cursors.Hand;
            // ButtonBase 基底仍会画一条 1px 的边框/背景线，这里彻底关掉
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.CheckedBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
        }

        /// <summary>OnPaint 已铺满整个客户区，禁止基底再画背景。</summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // 故意留空
        }

        /// <summary>勾选框边长（逻辑像素）。</summary>
        private const int BoxSize = 16;
        /// <summary>勾选框与文字之间的间距（逻辑像素）。</summary>
        private const int Gap = 8;

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Palette p = ThemeManager.Current;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int box = Metrics.Px(BoxSize);
            int radius = Metrics.Px(4);
            int top = (Height - box) / 2;
            Rectangle boxRect = new Rectangle(0, top, box - 1, box - 1);

            if (Checked)
            {
                Color accent = Enabled ? p.Accent : Palette.Mix(p.Accent, p.CardBg, 0.6f);
                Drawing.FillRoundedRect(g, boxRect, radius, accent);
                DrawCheckMark(g, boxRect, Enabled ? p.AccentText : p.TextDisabled);
            }
            else
            {
                Color fill = Enabled ? (_hover ? p.ControlHover : p.ControlBg) : Palette.Mix(p.ControlBg, p.CardBg, 0.5f);
                Color border = !Enabled ? p.CardBorder : (_hover ? p.CardBorderStrong : p.ControlBorder);
                Drawing.FillRoundedRect(g, boxRect, radius, fill, border, Metrics.Px(1));
            }

            int textLeft = box + Metrics.Px(Gap);
            Rectangle textRect = new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft), Height);
            Color textColor = Enabled ? p.Text : p.TextDisabled;
            Drawing.DrawText(g, Text, Font, textColor, textRect, ContentAlignment.MiddleLeft, clearType: false);
        }

        private void DrawCheckMark(Graphics g, Rectangle boxRect, Color color)
        {
            float w = Metrics.Px(2);
            using (Pen pen = new Pen(color, w))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                float x0 = boxRect.Left + boxRect.Width * 0.24f;
                float y0 = boxRect.Top + boxRect.Height * 0.52f;
                float x1 = boxRect.Left + boxRect.Width * 0.43f;
                float y1 = boxRect.Top + boxRect.Height * 0.72f;
                float x2 = boxRect.Left + boxRect.Width * 0.77f;
                float y2 = boxRect.Top + boxRect.Height * 0.29f;
                g.DrawLines(pen, new[] { new PointF(x0, y0), new PointF(x1, y1), new PointF(x2, y2) });
            }
        }
    }
}
