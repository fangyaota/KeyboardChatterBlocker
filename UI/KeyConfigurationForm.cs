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
    /// 单个按键的抖动阈值编辑对话框。
    /// </summary>
    public class KeyConfigurationForm : ModernForm
    {
        /// <summary>The key to configure.</summary>
        public Keys Key;

        /// <summary>Action to set the result.</summary>
        public Action<uint> SetResult;

        private TitleBar titleBar;
        private CardPanel card;
        private ModernLabel configureKeyLabel;
        private ModernLabel globalLabel;
        private ModernLabel wasLabel;
        private ModernNumericUpDown numericUpDown1;
        private ModernLabel promptLabel;
        private ModernLabel quickLabel;
        private ModernButton doneButton;
        private ModernButton cancelButton;

        private static readonly int[] Presets = { 30, 50, 80, 100, 150, 200 };

        public KeyConfigurationForm()
        {
            Size = new Size(Metrics.Px(520), Metrics.Px(360));
            MinimumSize = Size;
            MaximumSize = Size;
            BackColor = ThemeManager.Current.WindowBg;

            titleBar = new TitleBar
            {
                TitleText = Strings.KeyConfigTitle,
                SubtitleText = Strings.AppSubtitle,
                BarIcon = AppIcons.Small(),
            };

            card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(Metrics.Px(12)) };

            configureKeyLabel = new ModernLabel
            {
                Font = Fonts.Heading,
                Location = new Point(Metrics.Px(Metrics.CardPadding), Metrics.Px(Metrics.CardPadding) - Metrics.Px(2)),
                Size = new Size(Metrics.Px(430), Metrics.Px(30)),
            };

            globalLabel = new ModernLabel
            {
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(Metrics.Px(Metrics.CardPadding), Metrics.Px(Metrics.CardPadding) + Metrics.Px(32)),
                Size = new Size(Metrics.Px(430), Metrics.Px(20)),
            };

            wasLabel = new ModernLabel
            {
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(Metrics.Px(Metrics.CardPadding), Metrics.Px(Metrics.CardPadding) + Metrics.Px(54)),
                Size = new Size(Metrics.Px(430), Metrics.Px(20)),
            };

            promptLabel = new ModernLabel
            {
                Text = Strings.KeyConfigPrompt,
                Location = new Point(Metrics.Px(Metrics.CardPadding), Metrics.Px(Metrics.CardPadding) + Metrics.Px(92)),
                Size = new Size(Metrics.Px(430), Metrics.Px(22)),
            };

            numericUpDown1 = new ModernNumericUpDown
            {
                Minimum = 0,
                Maximum = 1000,
                Increment = 10,
                Location = new Point(Metrics.Px(Metrics.CardPadding), Metrics.Px(Metrics.CardPadding) + Metrics.Px(118)),
                Size = new Size(Metrics.Px(180), Metrics.Px(32)),
            };

            quickLabel = new ModernLabel
            {
                Text = Strings.KeyConfigQuick,
                Font = Fonts.Small,
                ForeColor = ThemeManager.Current.TextMuted,
                Location = new Point(Metrics.Px(Metrics.CardPadding) + Metrics.Px(200), Metrics.Px(Metrics.CardPadding) + Metrics.Px(118)),
                Size = new Size(Metrics.Px(60), Metrics.Px(32)),
            };

            int px = Metrics.Px(Metrics.CardPadding) + Metrics.Px(264);
            for (int i = 0; i < Presets.Length; i++)
            {
                int value = Presets[i];
                ModernButton preset = new ModernButton
                {
                    Text = value.ToString(),
                    Variant = ButtonVariant.Ghost,
                    Location = new Point(px + i * Metrics.Px(42), Metrics.Px(Metrics.CardPadding) + Metrics.Px(122)),
                    Size = new Size(Metrics.Px(38), Metrics.Px(26)),
                };
                preset.Click += (s, e) => numericUpDown1.Value = value;
                card.Controls.Add(preset);
            }

            cancelButton = new ModernButton
            {
                Text = Strings.Cancel,
                Location = new Point(Metrics.Px(Metrics.CardPadding), Metrics.Px(Metrics.CardPadding) + Metrics.Px(180)),
                Size = new Size(Metrics.Px(92), Metrics.Px(Metrics.ButtonHeight)),
            };
            cancelButton.Click += (s, e) => Close();

            doneButton = new ModernButton
            {
                Text = Strings.Done,
                Variant = ButtonVariant.Primary,
                Location = new Point(Metrics.Px(Metrics.CardPadding) + Metrics.Px(100), Metrics.Px(Metrics.CardPadding) + Metrics.Px(180)),
                Size = new Size(Metrics.Px(92), Metrics.Px(Metrics.ButtonHeight)),
            };
            doneButton.Click += DoneButton_Click;

            card.Controls.Add(doneButton);
            card.Controls.Add(cancelButton);
            card.Controls.Add(quickLabel);
            card.Controls.Add(numericUpDown1);
            card.Controls.Add(promptLabel);
            card.Controls.Add(wasLabel);
            card.Controls.Add(globalLabel);
            card.Controls.Add(configureKeyLabel);

            Controls.Add(card);
            Controls.Add(titleBar);

            AcceptButton = doneButton;
            CancelButton = cancelButton;
            Load += KeyConfigurationForm_Load;
        }

        /// <summary>
        /// Event method auto-called when the form is loaded.
        /// </summary>
        public void KeyConfigurationForm_Load(object sender, EventArgs e)
        {
            configureKeyLabel.Text = string.Format(Strings.KeyConfigConfigureFormat, KeyNames.Display(Key));
            globalLabel.Text = string.Format(Strings.KeyConfigGlobalFormat, Program.Blocker.GlobalChatterTimeLimit);
            uint curVal = Program.Blocker.KeysToChatterTime[Key] ?? Program.Blocker.GlobalChatterTimeLimit;
            wasLabel.Text = string.Format(Strings.KeyConfigWasFormat, curVal);
            numericUpDown1.Value = curVal;
            numericUpDown1.Focus();
        }

        /// <summary>
        /// Event method auto-called when the "Done" button is pressed.
        /// </summary>
        public void DoneButton_Click(object sender, EventArgs e)
        {
            SetResult?.Invoke((uint)numericUpDown1.Value);
            Close();
        }
    }
}
