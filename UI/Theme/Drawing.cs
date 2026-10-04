using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KeyboardChatterBlocker.UI.Theme
{
    /// <summary>自绘辅助方法。</summary>
    internal static class Drawing
    {
        /// <summary>构造一个圆角矩形路径。调用方负责 Dispose。</summary>
        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            // 半径过大时退化为圆/胶囊，避免路径自交
            int max = Math.Min(r.Width, r.Height) / 2;
            if (radius > max) { radius = max; }
            if (radius <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>填充并描边一个圆角矩形。</summary>
        public static void FillRoundedRect(Graphics g, Rectangle r, int radius, Color fill, Color? border = null, int borderWidth = 1)
        {
            using (GraphicsPath path = RoundedRect(r, radius))
            {
                if (fill.A > 0)
                {
                    using (SolidBrush b = new SolidBrush(fill)) { g.FillPath(b, path); }
                }
                if (border.HasValue && border.Value.A > 0 && borderWidth > 0)
                {
                    using (Pen p = new Pen(border.Value, borderWidth)) { g.DrawPath(p, path); }
                }
            }
        }

        /// <summary>填充并描边一个圆角矩形（Color 版本，无边框）。</summary>
        public static void FillRoundedRect(Graphics g, Rectangle r, int radius, Color fill)
        {
            FillRoundedRect(g, r, radius, fill, null, 0);
        }

        /// <summary>
        /// 绘制文本。
        /// 纯色背景下用 ClearType 最清晰；画在圆角/半透明卡片上必须用 AntiAlias，
        /// 否则 ClearType 会因为缺少不透明底色而出现黑色描边。
        /// </summary>
        public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle bounds,
            ContentAlignment align = ContentAlignment.MiddleLeft, bool clearType = false)
        {
            g.TextRenderingHint = clearType ? TextRenderingHint.ClearTypeGridFit : TextRenderingHint.AntiAliasGridFit;
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            switch (align)
            {
                case ContentAlignment.MiddleLeft:
                    flags |= TextFormatFlags.Left | TextFormatFlags.VerticalCenter; break;
                case ContentAlignment.MiddleCenter:
                    flags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter; break;
                case ContentAlignment.MiddleRight:
                    flags |= TextFormatFlags.Right | TextFormatFlags.VerticalCenter; break;
                case ContentAlignment.TopLeft:
                    flags |= TextFormatFlags.Left | TextFormatFlags.Top; break;
                case ContentAlignment.TopCenter:
                    flags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.Top; break;
                default:
                    flags |= TextFormatFlags.Left | TextFormatFlags.VerticalCenter; break;
            }
            TextRenderer.DrawText(g, text, font, bounds, color, flags);
        }

        /// <summary>
        /// 向上回溯父链，取第一个「不透明」的背景色。
        /// <para>
        /// 自绘控件常用 <c>Parent.BackColor</c> 铺底，但当父容器是
        /// <c>Color.Transparent</c> 的 TableLayoutPanel 时，直接取会得到透明色，
        /// 控件自身表面便回落到系统默认灰，形成突兀的浅色横带。
        /// </para>
        /// </summary>
        public static Color ParentBackColor(Control c, Color fallback)
        {
            for (Control p = c?.Parent; p != null; p = p.Parent)
            {
                Color bg = p.BackColor;
                if (bg.A > 0 && bg != Color.Transparent)
                {
                    return bg;
                }
            }
            return fallback;
        }

        /// <summary>按相对亮度自动选择黑或白前景色，用于强调色衬底上的文字。</summary>
        public static Color ContrastText(Color background)
        {
            double l = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
            return l > 0.6 ? Color.FromArgb(255, 24, 26, 30) : Color.White;
        }

        /// <summary>把颜色按比例调亮（t&gt;0）或调暗（t&lt;0）。</summary>
        public static Color Shade(Color c, float t)
        {
            if (t >= 0) { return Palette.Mix(c, Color.White, t); }
            return Palette.Mix(c, Color.Black, -t);
        }
    }
}
