using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 键盘布局图，实时高亮当前按下的键。
    /// <para>
    /// 按 ANSI 104 键定义（主键区 + 导航区 + 小键盘）。坐标统一用「1 个单位 = 一个标准字母键」，
    /// 所有键共用一个单位网格，因此各行的键在竖直方向天然对齐。
    /// </para>
    /// </summary>
    public class KeyboardMap : Control
    {
        private readonly struct KeyCap
        {
            public readonly string Label;
            public readonly Keys Vk;
            public readonly float X, Y;    // 单位网格上的左上角
            public readonly float W, H;    // 单位数

            public KeyCap(string label, Keys vk, float x, float y, float w, float h)
            {
                Label = label;
                Vk = vk;
                X = x;
                Y = y;
                W = w;
                H = h;
            }

            public KeyCap(string label, Keys vk, float x, float y, float w)
                : this(label, vk, x, y, w, 1f) { }
        }

        /// <summary>整张图的宽度（单位数）：主键区 15 + 间隙 0.5 + 导航区 3 + 间隙 0.5 + 小键盘 4。</summary>
        private const float MapW = 23f;
        /// <summary>行距（单位数）。键宽 1 单位、行距 1.32 —— 差的 0.32 就是行间隙。</summary>
        private const float RowStride = 1.32f;

        private static readonly KeyCap[] Caps = BuildLayout();

        private static KeyCap[] BuildLayout()
        {
            List<KeyCap> k = new List<KeyCap>();

            // ================= 主键区（15 单位宽） =================
            // 功能键行
            k.Add(new KeyCap("Esc", Keys.Escape, 0, 0, 1));
            for (int i = 0; i < 12; i++)
            {
                float x = 2f + i + (i >= 4 ? 0.5f : 0f) + (i >= 8 ? 0.5f : 0f);
                k.Add(new KeyCap("F" + (i + 1), (Keys)(0x70 + i), x, 0, 1));
            }

            // 数字行（VK 不连续：'1'..'9' 是 0x31..0x39，而 '0' 是 0x30）
            k.Add(new KeyCap("`", Keys.Oemtilde, 0, 1, 1));
            Keys[] digits = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0 };
            for (int i = 0; i < digits.Length; i++)
            {
                k.Add(new KeyCap(digits[i] == Keys.D0 ? "0" : (i + 1).ToString(), digits[i], 1 + i, 1, 1));
            }
            k.Add(new KeyCap("-", Keys.OemMinus, 11, 1, 1));
            k.Add(new KeyCap("=", Keys.Oemplus, 12, 1, 1));
            k.Add(new KeyCap("Bksp", Keys.Back, 13, 1, 2));

            // Tab 行
            k.Add(new KeyCap("Tab", Keys.Tab, 0, 2, 1.5f));
            Keys[] qwerty = { Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P };
            string[] qwertyLabels = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
            for (int i = 0; i < 10; i++)
            {
                k.Add(new KeyCap(qwertyLabels[i], qwerty[i], 1.5f + i, 2, 1));
            }
            k.Add(new KeyCap("[", Keys.OemOpenBrackets, 11.5f, 2, 1));
            k.Add(new KeyCap("]", Keys.OemCloseBrackets, 12.5f, 2, 1));
            k.Add(new KeyCap("\\", Keys.OemPipe, 13.5f, 2, 1.5f));

            // Caps 行
            k.Add(new KeyCap("Caps", Keys.Capital, 0, 3, 1.75f));
            Keys[] home = { Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L };
            string[] homeLabels = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
            for (int i = 0; i < 9; i++)
            {
                k.Add(new KeyCap(homeLabels[i], home[i], 1.75f + i, 3, 1));
            }
            k.Add(new KeyCap(";", Keys.OemSemicolon, 10.75f, 3, 1));
            k.Add(new KeyCap("'", Keys.OemQuotes, 11.75f, 3, 1));
            k.Add(new KeyCap("Enter", Keys.Enter, 12.75f, 3, 2.25f));

            // Shift 行
            k.Add(new KeyCap("Shift", Keys.LShiftKey, 0, 4, 2.25f));
            Keys[] bottom = { Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M };
            string[] bottomLabels = { "Z", "X", "C", "V", "B", "N", "M" };
            for (int i = 0; i < 7; i++)
            {
                k.Add(new KeyCap(bottomLabels[i], bottom[i], 2.25f + i, 4, 1));
            }
            k.Add(new KeyCap(",", Keys.Oemcomma, 9.25f, 4, 1));
            k.Add(new KeyCap(".", Keys.OemPeriod, 10.25f, 4, 1));
            k.Add(new KeyCap("/", Keys.OemQuestion, 11.25f, 4, 1));
            k.Add(new KeyCap("Shift", Keys.RShiftKey, 12.25f, 4, 2.75f));

            // 最底行
            k.Add(new KeyCap("Ctrl", Keys.LControlKey, 0, 5, 1.25f));
            k.Add(new KeyCap("Win", Keys.LWin, 1.25f, 5, 1.25f));
            k.Add(new KeyCap("Alt", Keys.LMenu, 2.5f, 5, 1.25f));
            k.Add(new KeyCap("Space", Keys.Space, 3.75f, 5, 6.25f));
            k.Add(new KeyCap("Alt", Keys.RMenu, 10f, 5, 1.25f));
            k.Add(new KeyCap("Win", Keys.RWin, 11.25f, 5, 1.25f));
            k.Add(new KeyCap("Menu", Keys.Apps, 12.5f, 5, 1.25f));
            k.Add(new KeyCap("Ctrl", Keys.RControlKey, 13.75f, 5, 1.25f));

            // ================= 导航区（x = 15.5） =================
            // 标签一律压在 3 个字符以内：1 单位宽的键在 150% DPI 下只有约 45px，
            // 而 7pt 字号下 "PgUp" 要 48px、"Home" 要 54px —— 4 个字符根本放不下。
            const float nav = 15.5f;
            k.Add(new KeyCap("Ins", Keys.Insert, nav, 1, 1));
            k.Add(new KeyCap("Hom", Keys.Home, nav + 1, 1, 1));
            k.Add(new KeyCap("Pg↑", Keys.Prior, nav + 2, 1, 1));
            k.Add(new KeyCap("Del", Keys.Delete, nav, 2, 1));
            k.Add(new KeyCap("End", Keys.End, nav + 1, 2, 1));
            k.Add(new KeyCap("Pg↓", Keys.Next, nav + 2, 2, 1));
            k.Add(new KeyCap("↑", Keys.Up, nav + 1, 4, 1));
            k.Add(new KeyCap("←", Keys.Left, nav, 5, 1));
            k.Add(new KeyCap("↓", Keys.Down, nav + 1, 5, 1));
            k.Add(new KeyCap("→", Keys.Right, nav + 2, 5, 1));

            // ================= 小键盘（x = 19） =================
            const float np = 19f;
            k.Add(new KeyCap("Num", Keys.NumLock, np, 1, 1));
            k.Add(new KeyCap("/", Keys.Divide, np + 1, 1, 1));
            k.Add(new KeyCap("*", Keys.Multiply, np + 2, 1, 1));
            k.Add(new KeyCap("-", Keys.Subtract, np + 3, 1, 1));

            k.Add(new KeyCap("7", Keys.NumPad7, np, 2, 1));
            k.Add(new KeyCap("8", Keys.NumPad8, np + 1, 2, 1));
            k.Add(new KeyCap("9", Keys.NumPad9, np + 2, 2, 1));
            k.Add(new KeyCap("+", Keys.Add, np + 3, 2, 1, 2f));

            k.Add(new KeyCap("4", Keys.NumPad4, np, 3, 1));
            k.Add(new KeyCap("5", Keys.NumPad5, np + 1, 3, 1));
            k.Add(new KeyCap("6", Keys.NumPad6, np + 2, 3, 1));

            k.Add(new KeyCap("1", Keys.NumPad1, np, 4, 1));
            k.Add(new KeyCap("2", Keys.NumPad2, np + 1, 4, 1));
            k.Add(new KeyCap("3", Keys.NumPad3, np + 2, 4, 1));
            k.Add(new KeyCap("Ent", Keys.Enter, np + 3, 4, 1, 2f));

            k.Add(new KeyCap("0", Keys.NumPad0, np, 5, 2));
            k.Add(new KeyCap(".", Keys.Decimal, np + 2, 5, 1));

            return k.ToArray();
        }

        private readonly HashSet<Keys> _down = new HashSet<Keys>();
        private readonly HashSet<Keys> _blockedDown = new HashSet<Keys>();

        /// <summary>
        /// 本次会话中「曾经被屏蔽拦下过」的按键。
        /// 与 <see cref="_blockedDown"/> 不同，它在按键松开后<b>不会</b>消失 ——
        /// 这样测完一轮以后，问题键会一直带着红框留在图上，一眼就能看出来。
        /// </summary>
        private readonly HashSet<Keys> _everBlocked = new HashSet<Keys>();

        /// <summary>当前被标记为「曾经被拦下」的按键数量。</summary>
        public int BlockedMarkCount => _everBlocked.Count;

        /// <summary>清除所有红色标记。</summary>
        public void ClearBlockedMarks()
        {
            if (_everBlocked.Count == 0)
            {
                return;
            }
            _everBlocked.Clear();
            if (Visible)
            {
                Invalidate();
            }
        }

        public KeyboardMap()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                // 可获焦点：这样站在本页时按键不会被侧边栏的数值输入框抢走
                | ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = ThemeManager.Current.CardBg;
        }

        /// <summary>
        /// 记录一次按键事件。<b>会被钩子线程调用，必须极快返回</b> ——
        /// 因此这里只改哈希集合，重绘交给下一帧。
        /// </summary>
        public void SetKeyState(Keys key, bool isDown, bool allowed)
        {
            if (isDown)
            {
                _down.Add(key);
                if (allowed)
                {
                    _blockedDown.Remove(key);
                }
                else
                {
                    _blockedDown.Add(key);
                    _everBlocked.Add(key);   // 留下永久标记，松手也不消失
                }
            }
            else
            {
                _down.Remove(key);
                _blockedDown.Remove(key);
            }
            if (Visible)
            {
                Invalidate();
            }
        }

        /// <summary>清空所有高亮（钩子被卸载时用，避免残留「按着」的假象）。</summary>
        public void ClearAll()
        {
            if (_down.Count == 0 && _blockedDown.Count == 0)
            {
                return;
            }
            _down.Clear();
            _blockedDown.Clear();
            if (Visible)
            {
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int pad = Metrics.Px(4);
            float availW = Width - pad * 2f;
            float availH = Height - pad * 2f;
            if (availW <= 0 || availH <= 0)
            {
                return;
            }

            float mapH = 5f * RowStride + 1f;
            // 单位宽度取横竖两个约束里较小的那个，保证整张图都塞得下
            float unit = Math.Min(availW / MapW, availH / mapH);
            if (unit < 4)
            {
                return;
            }

            float originX = pad + (availW - unit * MapW) / 2f;
            float originY = pad + (availH - unit * mapH) / 2f;

            float gap = Math.Max(1f, unit * 0.10f);
            int radius = Metrics.Px(3);
            int borderW = Math.Max(1, Metrics.Px(1));
            Font labelFont = unit < Metrics.Px(34) ? Fonts.Small : Fonts.Body;

            foreach (KeyCap cap in Caps)
            {
                RectangleF rf = new RectangleF(
                    originX + cap.X * unit,
                    originY + cap.Y * RowStride * unit,
                    cap.W * unit - gap,
                    (cap.H * RowStride - (RowStride - 1f)) * unit - gap);
                Rectangle rect = Rectangle.Round(rf);

                bool down = _down.Contains(cap.Vk);
                bool blocked = down && _blockedDown.Contains(cap.Vk);
                bool marked = !down && _everBlocked.Contains(cap.Vk);

                Color fill, border, text;
                if (blocked)
                {
                    // 物理上按下了，但被屏蔽吞掉、没进系统 —— 这正是最该被看见的状态
                    fill = p.Danger;
                    border = p.Danger;
                    text = Drawing.ContrastText(fill);
                }
                else if (down)
                {
                    fill = p.Accent;
                    border = p.Accent;
                    text = p.AccentText;
                }
                else if (marked)
                {
                    // 松手之后仍然留红框，方便事后一眼看出哪些键被拦过
                    fill = Palette.Mix(p.ControlBg, p.Danger, 0.10f);
                    border = p.Danger;
                    text = p.Text;
                }
                else
                {
                    fill = p.ControlBg;
                    border = p.ControlBorder;
                    text = p.TextMuted;
                }

                Drawing.FillRoundedRect(g, rect, radius, fill, border, borderW);
                if (!string.IsNullOrEmpty(cap.Label))
                {
                    // 窄键（导航区与小键盘的单宽键）用更小字号，否则 Home/PgUp/Num 会被截断成 Ho…/Pg…/Nu…
                    Font f = rect.Width < Metrics.Px(48) ? Fonts.Tiny : labelFont;
                    Drawing.DrawText(g, cap.Label, f, text, rect, ContentAlignment.MiddleCenter);
                }
            }
        }
    }
}
