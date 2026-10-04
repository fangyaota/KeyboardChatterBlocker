using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI;
using KeyboardChatterBlocker.UI.Controls;
using KeyboardChatterBlocker.UI.Localization;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// 等待用户按下一个按键，并把该键作为结果返回。
    /// </summary>
    public class NeedInputForm : ModernForm
    {
        /// <summary>An action to set the result key.</summary>
        public Action<Keys> SetResultKey;

        private TitleBar titleBar;
        private CardPanel card;
        private ModernLabel promptLabel;
        private ModernLabel warnLabel;
        private ModernComboBox alternateInputsBox;
        private ModernButton cancelButton;

        public NeedInputForm()
        {
            KeyPreview = true;
            Size = new Size(Metrics.Px(460), Metrics.Px(300));
            MinimumSize = Size;
            MaximumSize = Size;
            BackColor = ThemeManager.Current.WindowBg;

            titleBar = new TitleBar
            {
                TitleText = Strings.NeedInputTitle,
                SubtitleText = Strings.AppSubtitle,
                BarIcon = AppIcons.Small(),
            };

            card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(Metrics.Px(12)) };

            promptLabel = new ModernLabel
            {
                Text = Strings.NeedInputPrompt,
                Font = Fonts.Body,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = Metrics.Px(56),
            };

            warnLabel = new ModernLabel
            {
                Text = Strings.NeedInputAlreadyListed,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.Danger,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = Metrics.Px(22),
                Visible = false,
            };

            ModernLabel caption = new ModernLabel
            {
                Text = Strings.NeedInputAlternate,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Dock = DockStyle.Top,
                Height = Metrics.Px(24),
            };

            alternateInputsBox = new ModernComboBox { Dock = DockStyle.Top, Height = Metrics.Px(Metrics.InputHeight) };
            alternateInputsBox.Items.AddRange(new object[]
            {
                "Tab", "回车 (Enter)", "Esc", "空格",
                "鼠标左键", "鼠标右键", "鼠标中键 (M3)",
                "鼠标侧键前进 (M4)", "鼠标侧键后退 (M5)", "滚轮方向变化",
                "方向键 ↑", "方向键 ↓", "方向键 ←", "方向键 →",
            });
            alternateInputsBox.SelectedIndexChanged += AlternateInputsBox_SelectedIndexChanged;

            Panel spacer = new Panel { Dock = DockStyle.Top, Height = Metrics.Px(12), BackColor = ThemeManager.Current.CardBg };

            cancelButton = new ModernButton
            {
                Text = Strings.Cancel,
                Size = new Size(Metrics.Px(96), Metrics.Px(Metrics.ButtonHeight)),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            cancelButton.Click += CancelButton_Click;

            card.Controls.Add(cancelButton);
            card.Controls.Add(spacer);
            card.Controls.Add(alternateInputsBox);
            card.Controls.Add(caption);
            card.Controls.Add(warnLabel);
            card.Controls.Add(promptLabel);
            card.Resize += (s, e) => LayoutCancelButton();

            Controls.Add(card);
            Controls.Add(titleBar);

            Load += (s, e) =>
            {
                LayoutCancelButton();
                RejectMouseCapture(this);
                Activate();
                Focus();
            };
            KeyDown += NeedInputForm_KeyDown;
            FormClosed += (s, e) => alternateInputsBox?.Dispose();
        }

        private void LayoutCancelButton()
        {
            int pad = Metrics.Px(Metrics.CardPadding);
            cancelButton.Location = new Point(
                card.Width - cancelButton.Width - pad,
                card.Height - cancelButton.Height - pad);
        }

        /// <summary>
        /// 原版依靠窗体自身的 MouseDown 捕获鼠标按键。WinForms 的鼠标事件不会从子控件冒泡，
        /// 因此这里递归挂到所有子控件上，并排除「取消按钮」与下拉框所在的区域。
        /// </summary>
        private void RejectMouseCapture(Control root)
        {
            root.MouseDown += NeedInputForm_MouseDown;
            foreach (Control child in root.Controls)
            {
                RejectMouseCapture(child);
            }
        }

        /// <summary>
        /// Event method auto-called when the "Cancel" button is pressed.
        /// </summary>
        public void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Event method auto-called when any key is pressed within the form.
        /// </summary>
        public void NeedInputForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
                return;
            }
            if (Program.Blocker.KeysToChatterTime[e.KeyCode].HasValue)
            {
                warnLabel.Visible = true;
                return;
            }
            e.Handled = true;
            SetResultKey?.Invoke(e.KeyCode);
            Close();
        }

        /// <summary>
        /// Event method auto-called when any mouse button is pressed within the form.
        /// </summary>
        private void NeedInputForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Control c)
            {
                Point screenPt = c.PointToScreen(e.Location);
                if (cancelButton.RectangleToScreen(cancelButton.ClientRectangle).Contains(screenPt)
                    || alternateInputsBox.RectangleToScreen(alternateInputsBox.ClientRectangle).Contains(screenPt))
                {
                    return;
                }
            }
            Keys key = MouseKeyOf(e.Button);
            if (key == Keys.None)
            {
                return;
            }
            if (Program.Blocker.KeysToChatterTime[key].HasValue)
            {
                warnLabel.Visible = true;
                return;
            }
            SetResultKey?.Invoke(key);
            Close();
        }

        private static Keys MouseKeyOf(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.Left: return KeysHelper.KEY_MOUSE_LEFT;
                case MouseButtons.Right: return KeysHelper.KEY_MOUSE_RIGHT;
                case MouseButtons.Middle: return KeysHelper.KEY_MOUSE_MIDDLE;
                case MouseButtons.XButton1: return KeysHelper.KEY_MOUSE_BACKWARD;
                case MouseButtons.XButton2: return KeysHelper.KEY_MOUSE_FORWARD;
                default: return Keys.None;
            }
        }

        /// <summary>
        /// 下拉框选定特殊按键。
        /// 原版用 <c>switch (AlternateInputsBox.Text)</c> 硬编码英文项，中文化后必须按索引判定。
        /// </summary>
        private void AlternateInputsBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Keys key = Keys.None;
            switch (alternateInputsBox.SelectedIndex)
            {
                case 0: key = Keys.Tab; break;
                case 1: key = Keys.Enter; break;
                case 2: key = Keys.Escape; break;
                case 3: key = Keys.Space; break;
                case 4: key = KeysHelper.KEY_MOUSE_LEFT; break;
                case 5: key = KeysHelper.KEY_MOUSE_RIGHT; break;
                case 6: key = KeysHelper.KEY_MOUSE_MIDDLE; break;
                case 7: key = KeysHelper.KEY_MOUSE_FORWARD; break;
                case 8: key = KeysHelper.KEY_MOUSE_BACKWARD; break;
                case 9: key = KeysHelper.KEY_WHEEL_CHANGE; break;
                case 10: key = Keys.Up; break;
                case 11: key = Keys.Down; break;
                case 12: key = Keys.Left; break;
                case 13: key = Keys.Right; break;
                default: return;
            }
            SetResultKey?.Invoke(key);
            Close();
        }
    }
}
