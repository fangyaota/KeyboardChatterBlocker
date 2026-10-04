using System.Collections.Generic;
using System.Windows.Forms;

namespace KeyboardChatterBlocker.UI.Localization
{
    /// <summary>
    /// 键名 ↔ 中文显示名。
    /// <para>
    /// <b>重要</b>：日志表格与按键表格会把键名写进单元格，而双击处理又<b>从单元格文本反查按键</b>
    /// （原版用 <c>Enum.TryParse</c> / <c>KeysHelper.TryGetKey</c>）。因此凡是
    /// <see cref="Display"/> 会改写的键，<see cref="TryParseDisplay"/> 必须能反查回来。
    /// </para>
    /// config.txt 里始终写原始键名（<see cref="KeysHelper.Stringify"/>），不受此翻译影响。
    /// </summary>
    internal static class KeyNames
    {
        private static readonly Dictionary<Keys, string> ToDisplay = new Dictionary<Keys, string>
        {
            { KeysHelper.KEY_MOUSE_LEFT, "鼠标左键" },
            { KeysHelper.KEY_MOUSE_RIGHT, "鼠标右键" },
            { KeysHelper.KEY_MOUSE_MIDDLE, "鼠标中键" },
            { KeysHelper.KEY_MOUSE_FORWARD, "鼠标侧键前进" },
            { KeysHelper.KEY_MOUSE_BACKWARD, "鼠标侧键后退" },
            { KeysHelper.KEY_WHEEL_CHANGE, "滚轮方向变化" },
            { Keys.Return, "回车 (Enter)" },
            { Keys.Space, "空格" },
            { Keys.Escape, "Esc" },
            { Keys.Back, "退格 (Backspace)" },
            { Keys.Capital, "大写锁定" },
            { Keys.Up, "方向键 ↑" },
            { Keys.Down, "方向键 ↓" },
            { Keys.Left, "方向键 ←" },
            { Keys.Right, "方向键 →" },
        };

        private static readonly Dictionary<string, string> FromDisplay = BuildReverse();

        private static Dictionary<string, string> BuildReverse()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (KeyValuePair<Keys, string> pair in ToDisplay)
            {
                map[pair.Value] = KeysHelper.Stringify(pair.Key);
            }
            return map;
        }

        /// <summary>取按键的中文显示名。未收录的按键原样返回（如 <c>H</c>、<c>F5</c>）。</summary>
        public static string Display(Keys key)
        {
            return ToDisplay.TryGetValue(key, out string name) ? name : KeysHelper.Stringify(key);
        }

        /// <summary>
        /// 把单元格文本还原成按键。先按中文显示名反查，再回退到原始键名解析。
        /// </summary>
        public static bool TryParseDisplay(string display, out Keys key)
        {
            key = Keys.None;
            if (string.IsNullOrWhiteSpace(display))
            {
                return false;
            }
            string text = display.Trim();
            if (FromDisplay.TryGetValue(text, out string raw))
            {
                return KeysHelper.TryGetKey(raw, out key);
            }
            return KeysHelper.TryGetKey(text, out key);
        }
    }
}
