using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 自绘标签。与原生 <see cref="Label"/> 的区别：
    /// 会把 <c>BackColor</c> 渲染成圆角「胶囊」底色（把半透明色合成到父容器底色上），
    /// 从而保留原代码 <c>EnableNoteLabel.BackColor = Color.FromArgb(64, 255, 0, 0)</c> 的语义。
    /// </summary>
    public class ModernLabel : Label
    {
        public ModernLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Font = Fonts.Body;
            AutoSize = false;
            ForeColor = ThemeManager.Current.Text;
            BackColor = Color.Transparent;
        }

        /// <summary>是否为胶囊样式（有底色时圆角填充 + 内边距）。</summary>
        public bool Pill { get; set; }

        /// <summary>文字对齐。</summary>
        public new ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            Color parentBg = Drawing.ParentBackColor(this, p.CardBg);
            using (SolidBrush back = new SolidBrush(parentBg)) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool hasOwnBg = BackColor.A > 0 && BackColor != Color.Transparent && BackColor != parentBg;
            int textInset = 0;

            if (hasOwnBg)
            {
                // 半透明色合成到父底色上，得到实际可见颜色
                Color solid = Palette.Mix(parentBg, Color.FromArgb(255, BackColor), BackColor.A / 255f);
                int radius = Pill ? Height / 2 : Metrics.Px(Metrics.SmallRadius);
                int padX = Pill ? Metrics.Px(10) : Metrics.Px(6);
                Rectangle bgRect = new Rectangle(0, 0, Width - 1, Height - 1);
                Drawing.FillRoundedRect(g, bgRect, radius, solid);
                textInset = padX;
            }

            Rectangle textRect = new Rectangle(textInset, 0, System.Math.Max(0, Width - textInset * 2), Height);
            Color fg = Enabled ? ForeColor : p.TextDisabled;
            if (hasOwnBg && !NeedCustomForeColor()) { fg = ThemeManager.Current.IsDark ? p.Text : p.Text; }
            Drawing.DrawText(g, Text, Font, fg, textRect, TextAlign, clearType: false);
        }

        private bool NeedCustomForeColor()
        {
            return ForeColor != ThemeManager.Current.Text;
        }
    }
}
