using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// <see cref="ModernComboBox"/> 的弹出列表。自绘行、悬停高亮与细滚动条。
    /// </summary>
    internal sealed class DropDownListBox : Control
    {
        private readonly List<object> _items;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _scrollOffset;

        public DropDownListBox(List<object> items)
        {
            _items = items;
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Font = Fonts.Body;
            BackColor = ThemeManager.Current.CardBg;
        }

        /// <summary>行高（设备像素）。</summary>
        public int RowHeight { get; set; } = 30;

        /// <summary>是否需要显示滚动条。</summary>
        public bool ShowScroll { get; set; }

        /// <summary>某项被用户点击/回车确认。</summary>
        public event Action<int> ItemChosen;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                EnsureVisible(_selectedIndex);
                Invalidate();
            }
        }

        private int VisibleRows => Math.Max(1, Height / Math.Max(1, RowHeight));
        private int MaxOffset => Math.Max(0, _items.Count - VisibleRows);

        private void EnsureVisible(int index)
        {
            if (index < 0) { return; }
            if (index < _scrollOffset) { _scrollOffset = index; }
            else if (index >= _scrollOffset + VisibleRows) { _scrollOffset = index - VisibleRows + 1; }
            if (_scrollOffset < 0) { _scrollOffset = 0; }
            if (_scrollOffset > MaxOffset) { _scrollOffset = MaxOffset; }
        }

        /// <summary>键盘上下移动选中项。</summary>
        public void MoveSelection(int delta)
        {
            if (_items.Count == 0) { return; }
            int next = _selectedIndex + delta;
            if (next < 0) { next = 0; }
            if (next > _items.Count - 1) { next = _items.Count - 1; }
            SelectedIndex = next;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Down: e.Handled = true; MoveSelection(1); break;
                case Keys.Up: e.Handled = true; MoveSelection(-1); break;
                case Keys.PageDown: e.Handled = true; MoveSelection(VisibleRows); break;
                case Keys.PageUp: e.Handled = true; MoveSelection(-VisibleRows); break;
                case Keys.Home: e.Handled = true; SelectedIndex = 0; break;
                case Keys.End: e.Handled = true; SelectedIndex = _items.Count - 1; break;
                case Keys.Enter:
                case Keys.Space:
                    e.Handled = true;
                    if (_selectedIndex >= 0) { ItemChosen?.Invoke(_selectedIndex); }
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = IndexAt(e.Y);
            if (idx != _hoverIndex)
            {
                _hoverIndex = idx;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int idx = IndexAt(e.Y);
            if (idx >= 0) { ItemChosen?.Invoke(idx); }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int delta = e.Delta > 0 ? -1 : 1;
            _scrollOffset = Math.Max(0, Math.Min(MaxOffset, _scrollOffset + delta));
            Invalidate();
        }

        private int IndexAt(int y)
        {
            if (RowHeight <= 0) { return -1; }
            int idx = _scrollOffset + y / RowHeight;
            return idx >= 0 && idx < _items.Count ? idx : -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(p.CardBg)) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen border = new Pen(p.CardBorderStrong, Metrics.Px(1)))
            {
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }

            int scrollW = ShowScroll ? Metrics.Px(6) : 0;
            int textW = Width - Metrics.Px(20) - scrollW;

            int first = _scrollOffset;
            int last = Math.Min(_items.Count, first + VisibleRows + 1);
            for (int i = first; i < last; i++)
            {
                int y = (i - _scrollOffset) * RowHeight;
                Rectangle row = new Rectangle(0, y, Width, RowHeight);
                if (row.Bottom <= 0 || row.Top >= Height) { continue; }

                if (i == _selectedIndex)
                {
                    using (SolidBrush b = new SolidBrush(p.AccentSoft)) { g.FillRectangle(b, row); }
                }
                else if (i == _hoverIndex)
                {
                    using (SolidBrush b = new SolidBrush(p.ControlHover)) { g.FillRectangle(b, row); }
                }

                Rectangle textRect = new Rectangle(Metrics.Px(12), y, Math.Max(0, textW), RowHeight);
                bool emphasized = i == _selectedIndex;
                Drawing.DrawText(g, _items[i]?.ToString() ?? string.Empty,
                    emphasized ? Fonts.BodyBold : Font, p.Text, textRect, ContentAlignment.MiddleLeft, clearType: false);
            }

            if (ShowScroll && _items.Count > 0)
            {
                int trackX = Width - scrollW - Metrics.Px(2);
                int trackH = Height - Metrics.Px(4);
                float ratio = (float)VisibleRows / _items.Count;
                int thumbH = Math.Max(Metrics.Px(24), (int)(trackH * ratio));
                int travel = trackH - thumbH;
                int thumbY = Metrics.Px(2) + (MaxOffset == 0 ? 0 : travel * _scrollOffset / MaxOffset);

                using (SolidBrush thumb = new SolidBrush(p.ScrollThumb))
                {
                    g.FillRectangle(thumb, new Rectangle(trackX, thumbY, scrollW, thumbH));
                }
            }
        }
    }
}
