using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 输入框的圆角描边外壳。把 <see cref="ModernTextBox"/> 之类的无边框控件塞进来，
    /// 由外壳负责绘制边框、焦点态与内边距。
    /// </summary>
    public class FieldShell : Panel
    {
        public FieldShell()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            BackColor = ThemeManager.Current.ControlBg;
            Padding = new Padding(
                Metrics.Px(10),
                Metrics.Px(7),
                Metrics.Px(10),
                Metrics.Px(7));
        }

        /// <summary>是否处于错误态（红色边框）。</summary>
        public bool IsError { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int radius = Metrics.Px(Metrics.SmallRadius);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color border = IsError ? p.Danger : (ContainsFocus ? p.Accent : p.ControlBorder);
            Drawing.FillRoundedRect(g, r, radius, p.ControlBg, border, Metrics.Px(1));

            base.OnPaint(e);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            // 让内部控件填满留白区
            e.Control.Dock = DockStyle.Fill;
        }
    }
}
