using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>侧边栏导航项使用的矢量图标。</summary>
    public enum NavGlyph
    {
        Status, Log, Stats, Keys, AutoDisable, Settings, About
    }

    /// <summary>
    /// 侧边栏导航项：矢量图标 + 文本，选中态为强调色衬底 + 左侧色条。
    /// </summary>
    public class SideNavButton : Control
    {
        private bool _hover;
        private bool _selected;

        public SideNavButton(NavGlyph glyph, string text)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Glyph = glyph;
            Text = text;
            Height = Metrics.Px(Metrics.NavItemHeight);
            Font = Fonts.Body;
            Cursor = Cursors.Hand;
        }

        /// <summary>图标。</summary>
        public NavGlyph Glyph { get; set; }

        /// <summary>是否处于选中态。</summary>
        public bool Selected
        {
            get { return _selected; }
            set { if (_selected != value) { _selected = value; Invalidate(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(p.SidebarBg)) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int radius = Metrics.Px(Metrics.SmallRadius);
            int margin = Metrics.Px(8);
            Rectangle r = new Rectangle(margin, Metrics.Px(2), Width - margin * 2, Height - Metrics.Px(4));

            if (_selected)
            {
                Drawing.FillRoundedRect(g, r, radius, p.AccentSoft);
            }
            else if (_hover)
            {
                Drawing.FillRoundedRect(g, r, radius, p.ControlHover);
            }

            if (_selected)
            {
                // 左侧强调色条
                int barW = Metrics.Px(3);
                int barH = Height - Metrics.Px(14);
                Rectangle bar = new Rectangle(Metrics.Px(1), (Height - barH) / 2, barW, barH);
                Drawing.FillRoundedRect(g, bar, barW / 2, p.Accent);
            }

            Color fg = _selected ? p.Accent : p.Text;
            int iconSize = Metrics.Px(16);
            int left = Metrics.Px(20);
            Rectangle iconRect = new Rectangle(left, (Height - iconSize) / 2, iconSize, iconSize);
            DrawGlyph(g, iconRect, fg);

            int textLeft = iconRect.Right + Metrics.Px(10);
            Rectangle textRect = new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft - margin), Height);
            Drawing.DrawText(g, Text, _selected ? Fonts.BodyBold : Font, fg, textRect, ContentAlignment.MiddleLeft, clearType: false);
        }

        private void DrawGlyph(Graphics g, Rectangle r, Color color)
        {
            using (Pen pen = new Pen(color, Metrics.Pxf(1.5f)))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                float x = r.Left, y = r.Top, w = r.Width, h = r.Height;
                switch (Glyph)
                {
                    case NavGlyph.Status:
                    {
                        float s = w * 0.42f;
                        g.DrawRectangle(pen, x, y, s, s);
                        g.DrawRectangle(pen, x + w - s, y, s, s);
                        g.DrawRectangle(pen, x, y + h - s, s, s);
                        g.DrawRectangle(pen, x + w - s, y + h - s, s, s);
                        break;
                    }
                    case NavGlyph.Log:
                    {
                        float gap = h / 4f;
                        for (int i = 1; i <= 3; i++)
                        {
                            float ly = y + gap * i;
                            float lw = i == 2 ? w * 0.65f : w;
                            g.DrawLine(pen, x, ly, x + lw, ly);
                        }
                        break;
                    }
                    case NavGlyph.Stats:
                    {
                        float bw = w / 4f;
                        g.DrawLine(pen, x + bw * 0.5f, y + h, x + bw * 0.5f, y + h * 0.35f);
                        g.DrawLine(pen, x + bw * 2.0f, y + h, x + bw * 2.0f, y + h * 0.05f);
                        g.DrawLine(pen, x + bw * 3.5f, y + h, x + bw * 3.5f, y + h * 0.55f);
                        break;
                    }
                    case NavGlyph.Keys:
                    {
                        Rectangle board = new Rectangle((int)x, (int)(y + h * 0.2f), (int)w, (int)(h * 0.6f));
                        using (System.Drawing.Drawing2D.GraphicsPath path = Drawing.RoundedRect(board, Metrics.Px(2)))
                        {
                            g.DrawPath(pen, path);
                        }
                        using (SolidBrush b = new SolidBrush(color))
                        {
                            float dot = Math.Max(1f, w * 0.10f);
                            for (int i = 0; i < 3; i++)
                            {
                                g.FillEllipse(b, x + w * 0.18f + i * w * 0.28f, y + h * 0.42f, dot, dot);
                            }
                        }
                        break;
                    }
                    case NavGlyph.AutoDisable:
                    {
                        g.DrawEllipse(pen, x, y, w, h);
                        g.DrawLine(pen, x + w * 0.2f, y + h * 0.8f, x + w * 0.8f, y + h * 0.2f);
                        break;
                    }
                    case NavGlyph.Settings:
                    {
                        float cx = x + w / 2f, cy = y + h / 2f;
                        float rr = w * 0.30f;
                        g.DrawEllipse(pen, cx - rr, cy - rr, rr * 2, rr * 2);
                        for (int i = 0; i < 6; i++)
                        {
                            double ang = Math.PI / 3 * i;
                            float x1 = cx + (float)Math.Cos(ang) * rr * 1.35f;
                            float y1 = cy + (float)Math.Sin(ang) * rr * 1.35f;
                            float x2 = cx + (float)Math.Cos(ang) * rr * 1.9f;
                            float y2 = cy + (float)Math.Sin(ang) * rr * 1.9f;
                            g.DrawLine(pen, x1, y1, x2, y2);
                        }
                        break;
                    }
                    case NavGlyph.About:
                    {
                        g.DrawEllipse(pen, x, y, w, h);
                        using (SolidBrush b = new SolidBrush(color))
                        {
                            float dot = Math.Max(1.5f, w * 0.11f);
                            g.FillEllipse(b, x + w / 2f - dot / 2, y + h * 0.22f, dot, dot);
                        }
                        g.DrawLine(pen, x + w / 2f, y + h * 0.45f, x + w / 2f, y + h * 0.76f);
                        break;
                    }
                }
            }
        }
    }
}
