using System;
using System.Runtime.InteropServices;

namespace KeyboardChatterBlocker.UI.Theme
{
    /// <summary>
    /// 布局度量常量，全部为 **96 DPI 下的逻辑像素**。
    /// <para>
    /// 本项目的界面完全由代码构建，而 WinForms 的 <c>AutoScaleDimensions</c> 在
    /// PerMonitorV2 下会被框架改写成「当前 DPI」，导致自动缩放因子恒为 1
    /// （控件不缩放、文字却按高 DPI 渲染 → 文字撑破布局）。
    /// 因此这里关闭框架自动缩放，改由 <see cref="Px"/> 统一换算，
    /// 保证布局与文字在同一坐标系里。
    /// </para>
    /// </summary>
    internal static class Metrics
    {
        // —— 96 DPI 基准的逻辑尺寸 ——
        public const int CornerRadius = 8;
        public const int SmallRadius = 6;

        public const int CardPadding = 16;
        public const int CardGap = 12;

        public const int TitleBarHeight = 44;
        public const int SidebarWidth = 236;

        public const int ButtonHeight = 32;
        public const int NavItemHeight = 38;
        public const int InputHeight = 32;

        public const int GridHeaderHeight = 34;
        public const int GridRowHeight = 30;

        public const int ResizeGrip = 6;

        /// <summary>启动时确定的系统 DPI（默认 96）。</summary>
        public static int Dpi { get; private set; } = 96;

        /// <summary>相对 96 DPI 的缩放系数。</summary>
        public static float Factor => Dpi / 96f;

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        /// <summary>
        /// 必须在创建任何控件之前调用。
        /// </summary>
        public static void Initialize()
        {
            try
            {
                uint dpi = GetDpiForSystem();
                if (dpi >= 48 && dpi <= 480)
                {
                    Dpi = (int)dpi;
                }
            }
            catch
            {
                // 老系统上取不到就按 96 处理
                Dpi = 96;
            }
        }

        /// <summary>把逻辑像素换算成设备像素。所有布局与自绘几何都必须经过它。</summary>
        public static int Px(int logical)
        {
            return (int)Math.Round(logical * Factor);
        }

        /// <summary>把逻辑长度换算成设备长度并保留小数（用于画笔宽度）。</summary>
        public static float Pxf(float logical)
        {
            return logical * Factor;
        }
    }
}
