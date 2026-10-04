using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 自绘开关（胶囊轨道 + 滑块）。
    /// 继承 <see cref="CheckBox"/>，因此 <c>.Checked</c> / <c>.CheckedChanged</c> / <c>.Text</c> 与原代码完全兼容。
    /// </summary>
    public class ToggleSwitch : CheckBox
    {
        private bool _hover;
        private float _anim;      // 0 = 关，1 = 开（用于滑块过渡）
        private readonly Timer _animTimer;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Font = Fonts.Body;
            AutoSize = false;
            Height = Metrics.Px(24);
            Cursor = Cursors.Hand;
            _anim = Checked ? 1f : 0f;

            _animTimer = new Timer { Interval = 15 };
            _animTimer.Tick += (s, e) => StepAnimation();
        }

        /// <summary>开关是否绘制在控件右侧（文字在左）。默认在左侧。</summary>
        public bool SwitchOnRight { get; set; }

        private const int TrackWidth = 40;
        private const int TrackHeight = 22;
        private const int Gap = 8;

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            if (IsHandleCreated) { _animTimer.Start(); }
            else { _anim = Checked ? 1f : 0f; }
            Invalidate();
        }

        private void StepAnimation()
        {
            float target = Checked ? 1f : 0f;
            float diff = target - _anim;
            if (Math.Abs(diff) < 0.08f)
            {
                _anim = target;
                _animTimer.Stop();
            }
            else
            {
                _anim += diff * 0.35f;
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _animTimer?.Dispose(); }
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Palette p = ThemeManager.Current;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int tw = Metrics.Px(TrackWidth);
            int th = Metrics.Px(TrackHeight);
            int gap = Metrics.Px(Gap);
            int trackTop = (Height - th) / 2;
            int trackLeft = SwitchOnRight ? Math.Max(0, Width - tw) : 0;
            Rectangle track = new Rectangle(trackLeft, trackTop, tw, th);

            Color offColor = Enabled ? p.TrackOff : Palette.Mix(p.TrackOff, p.CardBg, 0.5f);
            Color onColor = Enabled ? p.Accent : Palette.Mix(p.Accent, p.CardBg, 0.6f);
            Color trackColor = Palette.Mix(offColor, onColor, _anim);

            Drawing.FillRoundedRect(g, track, th / 2, trackColor);

            int knobPad = Metrics.Px(3);
            int knobD = th - knobPad * 2;
            int travel = tw - knobD - knobPad * 2;
            int knobX = track.Left + knobPad + (int)(travel * _anim);
            Rectangle knob = new Rectangle(knobX, track.Top + knobPad, knobD, knobD);

            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(_hover ? 60 : 40, 0, 0, 0)))
            {
                g.FillEllipse(shadow, new Rectangle(knob.X, knob.Y + Metrics.Px(1), knob.Width, knob.Height));
            }
            using (SolidBrush knobBrush = new SolidBrush(Enabled ? Color.White : Color.FromArgb(220, 240, 240, 240)))
            {
                g.FillEllipse(knobBrush, knob);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                Rectangle textRect;
                if (SwitchOnRight)
                {
                    textRect = new Rectangle(0, 0, Math.Max(0, track.Left - gap), Height);
                    Drawing.DrawText(g, Text, Font, Enabled ? p.Text : p.TextDisabled, textRect, ContentAlignment.MiddleLeft, clearType: false);
                }
                else
                {
                    int left = track.Right + gap;
                    textRect = new Rectangle(left, 0, Math.Max(0, Width - left), Height);
                    Drawing.DrawText(g, Text, Font, Enabled ? p.Text : p.TextDisabled, textRect, ContentAlignment.MiddleLeft, clearType: false);
                }
            }
        }
    }
}
