using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyboardChatterBlocker.UI.Theme
{
    /// <summary>
    /// 全局主题。跟随系统「应用模式」亮/暗设置，
    /// <b>不持久化</b>——因为 config.txt 的键名与格式不允许变动。
    /// </summary>
    internal static class ThemeManager
    {
        /// <summary>当前配色。控件应在 OnPaint 里实时读取，不要缓存到字段。</summary>
        public static Palette Current { get; private set; } = Palette.Light();

        /// <summary>当前是否为暗色。</summary>
        public static bool IsDark => Current.IsDark;

        /// <summary>主题变更事件，由 <see cref="Apply"/> 触发。</summary>
        public static event Action ThemeChanged;

        /// <summary>读取注册表判定系统是否处于暗色「应用模式」。</summary>
        public static bool IsSystemDark()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key?.GetValue("AppsUseLightTheme");
                    if (value is int i) { return i == 0; }
                }
            }
            catch
            {
                // 读不到就按亮色处理，不影响功能
            }
            return false;
        }

        /// <summary>按系统设置初始化。必须在创建任何窗口之前调用。</summary>
        public static void Initialize()
        {
            Current = IsSystemDark() ? Palette.Dark() : Palette.Light();
        }

        /// <summary>递归把主题应用到控件树，并广播 <see cref="ThemeChanged"/>。</summary>
        public static void Apply(Control root)
        {
            if (root == null) { return; }
            ApplyToControl(root);
            foreach (Control child in root.Controls)
            {
                Apply(child);
            }
            ThemeChanged?.Invoke();
        }

        private static void ApplyToControl(Control c)
        {
            switch (c)
            {
                case DataGridView _:
                case TextBoxBase _:
                case ListView _:
                case ComboBox _:
                    // 这些控件自带完整的主题逻辑（或需要保留自绘），跳过统一覆写
                    break;
                default:
                    c.BackColor = Current.CardBg;
                    c.ForeColor = Current.Text;
                    break;
            }
        }
    }
}
