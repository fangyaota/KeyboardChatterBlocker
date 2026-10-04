using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 圆角卡片容器。可选标题，子控件自行用 Padding 留白。
    /// </summary>
    public class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            // 让子控件的「透明」背景能取到卡片底色
            BackColor = ThemeManager.Current.CardBg;
            Font = Fonts.Body;
            Padding = new Padding(
                Metrics.Px(Metrics.CardPadding),
                Metrics.Px(Metrics.CardPadding),
                Metrics.Px(Metrics.CardPadding),
                Metrics.Px(Metrics.CardPadding));
        }

        /// <summary>卡片标题，为空则不绘制。</summary>
        public string Title { get; set; }

        /// <summary>是否绘制边框（嵌在另一张卡片里时可关掉）。</summary>
        public bool ShowBorder { get; set; } = true;

        /// <summary>是否使用更弱的边框（次级卡片）。</summary>
        public bool Subtle { get; set; }

        /// <summary>标题占用的高度（逻辑像素），供布局参考。</summary>
        public int TitleHeight => string.IsNullOrEmpty(Title) ? 0 : Metrics.Px(30);

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.WindowBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int radius = Metrics.Px(Metrics.CornerRadius);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color border = ShowBorder ? (Subtle ? Color.FromArgb(120, p.CardBorder) : p.CardBorder) : Color.Transparent;
            Drawing.FillRoundedRect(g, r, radius, p.CardBg, border, border.A > 0 ? Metrics.Px(1) : 0);

            if (!string.IsNullOrEmpty(Title))
            {
                int pad = Metrics.Px(Metrics.CardPadding);
                Rectangle titleRect = new Rectangle(pad, pad, Math.Max(0, Width - pad * 2), Metrics.Px(22));
                Drawing.DrawText(g, Title, Fonts.BodyBold, p.Text, titleRect, ContentAlignment.MiddleLeft, clearType: false);
            }

            base.OnPaint(e);
        }
    }
}
