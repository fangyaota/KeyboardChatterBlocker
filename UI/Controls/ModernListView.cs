using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 自绘的 <see cref="ListView"/>（列表视图）。
    /// 原 UI 用 <c>item.BackColor</c> 表示「该进程正在导致自动禁用」的红色高亮——
    /// OwnerDraw 下直接读 <c>e.Item.BackColor</c>，因此该行为得以保留。
    /// </summary>
    public class ModernListView : ListView
    {
        public ModernListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
            OwnerDraw = true;
            // 原版用 View.List（无列）。这里改用 Details + 单列自适应宽度，
            // 让每个程序名独占一整行，便于展示高亮底色与长程序名。
            View = View.Details;
            HeaderStyle = ColumnHeaderStyle.None;
            Columns.Add(string.Empty, -2, HorizontalAlignment.Left);
            FullRowSelect = true;
            MultiSelect = false;
            HideSelection = false;
            BorderStyle = BorderStyle.None;
            Font = Fonts.Body;
            BackColor = ThemeManager.Current.GridBg;
            DrawItem += ModernListView_DrawItem;
            DrawColumnHeader += (s, e) => { };
            // 单列强制铺满，否则原生控件会在列宽处画一条竖直分隔线
            Resize += (s, e) => SyncColumnWidth();
            SyncColumnWidth();
        }

        private void SyncColumnWidth()
        {
            if (Columns.Count > 0 && ClientSize.Width > 0)
            {
                Columns[0].Width = ClientSize.Width;
            }
        }

        private void ModernListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 尊重调用方设置的 BackColor（原代码用它做命中高亮）；
            // 未设置时退回表格底色。
            Color bg = e.Item.BackColor;
            if (bg == Color.Transparent || bg.IsEmpty) { bg = p.GridBg; }

            bool selected = e.Item.Selected;
            Rectangle r = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);

            if (selected)
            {
                using (SolidBrush b = new SolidBrush(p.GridSelBg)) { g.FillRectangle(b, r); }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(bg)) { g.FillRectangle(b, r); }
            }

            // 底部分隔线
            using (Pen pen = new Pen(p.GridLine, Metrics.Px(1)))
            {
                g.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
            }

            int pad = Metrics.Px(10);
            Rectangle textRect = new Rectangle(r.Left + pad, r.Top, r.Width - pad * 2, r.Height);
            Color fg = selected ? p.GridSelText : (IsBadHighlight(bg) ? Color.White : p.Text);
            Drawing.DrawText(g, e.Item.Text, Font, fg, textRect, ContentAlignment.MiddleLeft, clearType: false);
        }

        private static bool IsBadHighlight(Color c)
        {
            // 原版高亮色为 Color.FromArgb(255, 128, 128)，属深色底，需要白字
            double l = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return l < 0.6;
        }
    }
}
