using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 换肤后的单行输入框。本身无边框，圆角边框由外层 <see cref="FieldShell"/> 绘制。
    /// 继承 <see cref="TextBox"/> 以保证 <c>.Text</c> / <c>.TextChanged</c> / <c>.Bounds</c> 语义不变。
    /// </summary>
    public class ModernTextBox : TextBox
    {
        public ModernTextBox()
        {
            BorderStyle = BorderStyle.None;
            Font = Fonts.Body;
            ApplyTheme();
        }

        /// <summary>
        /// 为 true 时不覆盖 <see cref="Control.ForeColor"/>，
        /// 便于调用方用弱化色显示占位文本。
        /// </summary>
        public bool PreserveForeColor { get; set; }

        /// <summary>按当前主题刷新配色。</summary>
        public void ApplyTheme()
        {
            Palette p = ThemeManager.Current;
            BackColor = p.ControlBg;
            if (!PreserveForeColor)
            {
                ForeColor = p.Text;
            }
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            // 无边框输入框在暗色下的插入符颜色跟随系统，这里设一次即可
            ApplyTheme();
        }
    }
}
