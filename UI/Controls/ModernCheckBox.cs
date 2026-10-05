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
        private bool _foreignMark;

        /// <summary>
        /// 「这件事有人做了，但不是我们」的标记：不画勾，改画一个实心方块。
        /// <para>
        /// 用在「开机自启」上 —— 启动文件夹里那个 .lnk 存在，但指向的是别的程序
        /// （同名快捷方式被别的软件占了）。此时 <see cref="CheckBox.Checked"/> 保持
        /// <c>false</c>，让用户一眼看出「没勾是因为不是我注册的」，
        /// 而不是「我没开」。点一下仍然会把快捷方式改成指向本程序。
        /// </para>
        /// </summary>
        public bool ForeignMark
        {
            get { return _foreignMark; }
            set
            {
                if (_foreignMark == value) { return; }
                _foreignMark = value;
                Invalidate();
            }
        }

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
            else if (_foreignMark)
            {
                // 空勾选框 + 正中一枚实心方块：看得出「这里有事」，但没有打勾
                Color border = Enabled ? p.CardBorderStrong : p.CardBorder;
                Drawing.FillRoundedRect(g, boxRect, radius, p.ControlBg, border, Metrics.Px(1));
                DrawSquareMark(g, boxRect, Enabled ? p.TextMuted : p.TextDisabled);
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

        /// <summary>「不是我们注册的」标记：勾选框正中一枚实心方块。</summary>
        private void DrawSquareMark(Graphics g, Rectangle boxRect, Color color)
        {
            int w = Math.Max(2, (int)(boxRect.Width * 0.42f));
            int x = boxRect.Left + (boxRect.Width - w) / 2;
            int y = boxRect.Top + (boxRect.Height - w) / 2;
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y, w, w);
            }
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
