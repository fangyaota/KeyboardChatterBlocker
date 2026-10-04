using System.Drawing;
using System.Drawing.Text;

namespace KeyboardChatterBlocker.UI.Theme
{
    /// <summary>
    /// 字体阶梯。优先使用「微软雅黑 UI」，缺失时回退到系统默认无衬线字体。
    /// 全部为应用生命周期内的静态字体，不随主题切换变化。
    /// </summary>
    internal static class Fonts
    {
        private static readonly string Family = ResolveFamily();

        /// <summary>正文（界面默认）。</summary>
        public static readonly Font Body = new Font(Family, 9F, FontStyle.Regular, GraphicsUnit.Point);
        /// <summary>正文加粗，用于强调的数值。</summary>
        public static readonly Font BodyBold = new Font(Family, 9F, FontStyle.Bold, GraphicsUnit.Point);
        /// <summary>小号说明文字。</summary>
        public static readonly Font Small = new Font(Family, 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        /// <summary>页面标题。</summary>
        public static readonly Font Heading = new Font(Family, 12F, FontStyle.Bold, GraphicsUnit.Point);
        /// <summary>标题栏应用名。</summary>
        public static readonly Font Title = new Font(Family, 10F, FontStyle.Bold, GraphicsUnit.Point);
        /// <summary>大号数值（概览卡片里的阈值）。</summary>
        public static readonly Font Numeric = new Font(Family, 14F, FontStyle.Bold, GraphicsUnit.Point);
        /// <summary>更小号，用于键盘图上那些窄键（Home / PgUp / Num 之类）。</summary>
        public static readonly Font Tiny = new Font(Family, 7F, FontStyle.Regular, GraphicsUnit.Point);
        /// <summary>等宽，用于日志时间戳等需要对齐的列。</summary>
        public static readonly Font Mono = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);

        private static string ResolveFamily()
        {
            string[] preferred = { "Microsoft YaHei UI", "Microsoft YaHei", "微软雅黑", "Segoe UI" };
            using (InstalledFontCollection installed = new InstalledFontCollection())
            {
                foreach (string want in preferred)
                {
                    foreach (FontFamily fam in installed.Families)
                    {
                        if (string.Equals(fam.Name, want, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return want;
                        }
                    }
                }
            }
            return SystemFonts.MessageBoxFont.FontFamily.Name;
        }
    }
}
