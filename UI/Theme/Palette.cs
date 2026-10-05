using System.Drawing;

namespace KeyboardChatterBlocker.UI.Theme
{
    /// <summary>
    /// 一套主题配色。所有颜色以只读字段形式暴露，控件在 OnPaint 里实时取色（不要在构造时缓存）。
    /// </summary>
    public sealed class Palette
    {
        /// <summary>是否为暗色主题。</summary>
        public bool IsDark;

        // —— 窗口 / 容器 ——
        public Color WindowBg;
        public Color SidebarBg;
        public Color CardBg;
        public Color CardBorder;
        public Color CardBorderStrong;

        // —— 文本 ——
        public Color Text;
        public Color TextMuted;
        public Color TextDisabled;

        // —— 强调色 ——
        public Color Accent;
        public Color AccentHover;
        public Color AccentPressed;
        public Color AccentText;
        /// <summary>强调色的低透明度衬底（用于选中项背景、徽标底色）。</summary>
        public Color AccentSoft;

        // —— 成功色（键盘测试的「良好」状态）——
        public Color Success;
        public Color SuccessSoft;

        // —— 危险色（自动禁用提示等）——
        public Color Danger;
        public Color DangerSoft;
        public Color DangerText;

        // —— 表格 ——
        public Color GridBg;
        public Color GridAltBg;
        public Color GridHeaderBg;
        public Color GridLine;
        public Color GridSelBg;
        public Color GridSelText;

        // —— 通用控件 ——
        public Color ControlBg;
        public Color ControlBorder;
        public Color ControlHover;
        public Color ControlPressed;
        public Color TrackOff;
        public Color ScrollThumb;

        /// <summary>暗色主题。</summary>
        public static Palette Dark()
        {
            return new Palette
            {
                IsDark = true,
                WindowBg = Hex(0x1F2126),
                SidebarBg = Hex(0x191B1F),
                CardBg = Hex(0x262A31),
                CardBorder = Hex(0x33383F),
                CardBorderStrong = Hex(0x454C55),

                Text = Hex(0xE6E8EB),
                TextMuted = Hex(0x9AA1AA),
                TextDisabled = Hex(0x6B7280),

                Accent = Hex(0x4C8DFF),
                AccentHover = Hex(0x6BA1FF),
                AccentPressed = Hex(0x3A7AE8),
                AccentText = Hex(0xFFFFFF),
                AccentSoft = Hex(0x22304A),

                Success = Hex(0x3FB950),
                SuccessSoft = Hex(0x1B3226),

                Danger = Hex(0xFF5C5C),
                DangerSoft = Hex(0x45262A),
                DangerText = Hex(0xFFA0A0),

                GridBg = Hex(0x22262C),
                GridAltBg = Hex(0x262A31),
                GridHeaderBg = Hex(0x2C3138),
                GridLine = Hex(0x33383F),
                GridSelBg = Hex(0x2F4A7A),
                GridSelText = Hex(0xFFFFFF),

                ControlBg = Hex(0x2E333A),
                ControlBorder = Hex(0x3D434B),
                ControlHover = Hex(0x383E46),
                ControlPressed = Hex(0x2A2F36),
                TrackOff = Hex(0x3D434B),
                ScrollThumb = Hex(0x4A515A),
            };
        }

        /// <summary>亮色主题。</summary>
        public static Palette Light()
        {
            return new Palette
            {
                IsDark = false,
                WindowBg = Hex(0xF5F6F8),
                SidebarBg = Hex(0xFFFFFF),
                CardBg = Hex(0xFFFFFF),
                CardBorder = Hex(0xE3E6EA),
                CardBorderStrong = Hex(0xC8CDD4),

                Text = Hex(0x1B1D21),
                TextMuted = Hex(0x6B7280),
                TextDisabled = Hex(0xA0A6AE),

                Accent = Hex(0x2563EB),
                AccentHover = Hex(0x3B78F0),
                AccentPressed = Hex(0x1D4FD8),
                AccentText = Hex(0xFFFFFF),
                AccentSoft = Hex(0xDCE9FF),

                Success = Hex(0x1A7F37),
                SuccessSoft = Hex(0xE6F4EA),

                Danger = Hex(0xDC2626),
                DangerSoft = Hex(0xFEE9E9),
                DangerText = Hex(0xB91C1C),

                GridBg = Hex(0xFFFFFF),
                GridAltBg = Hex(0xFAFBFC),
                GridHeaderBg = Hex(0xF1F3F5),
                GridLine = Hex(0xE8EBEF),
                GridSelBg = Hex(0xDCE9FF),
                GridSelText = Hex(0x14315E),

                ControlBg = Hex(0xFFFFFF),
                ControlBorder = Hex(0xD6DAE0),
                ControlHover = Hex(0xF0F2F5),
                ControlPressed = Hex(0xE4E7EB),
                TrackOff = Hex(0xCBD2DA),
                ScrollThumb = Hex(0xB6BDC6),
            };
        }

        private static Color Hex(int rgb)
        {
            return Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        /// <summary>按比例混合两个颜色，<paramref name="t"/> 为 0 返回 a、为 1 返回 b。</summary>
        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) { t = 0f; }
            if (t > 1f) { t = 1f; }
            return Color.FromArgb(
                a.A + (int)((b.A - a.A) * t),
                a.R + (int)((b.R - a.R) * t),
                a.G + (int)((b.G - a.G) * t),
                a.B + (int)((b.B - a.B) * t));
        }
    }
}
