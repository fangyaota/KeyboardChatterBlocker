using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 完全自绘的下拉框。原生 <see cref="ComboBox"/> 的箭头与边框由系统绘制、无法换肤，
    /// 因此这里用 <see cref="Control"/> + <see cref="ToolStripDropDown"/> 自己实现。
    /// <para>
    /// 兼容原代码用到的成员：<c>Items</c> / <c>SelectedIndex</c> / <c>SelectedIndexChanged</c> /
    /// <c>Text</c> / <c>Bounds</c>。
    /// </para>
    /// </summary>
    public class ModernComboBox : Control
    {
        private readonly List<object> _items = new List<object>();
        private readonly ItemCollection _itemCollection;
        private readonly ToolStripDropDown _dropDown;
        private readonly DropDownListBox _listBox;
        private int _selectedIndex = -1;
        private bool _hover;
        private bool _open;
        private bool _focusable = true;
        private bool _yieldingFocus;

        /// <summary>
        /// 是否接受焦点，默认 <c>true</c>。
        /// <para>
        /// 设为 <c>false</c> 后只能用鼠标点开：Tab 跳不过去，方向键/空格也不会被它吃掉。
        /// 「键盘测试」页的筛选框需要这个 —— 那一页的每一次按键都该给键盘图。
        /// </para>
        /// <para>做法与 <see cref="ModernButton"/> / <see cref="ModernDataGridView"/> 一致：
        /// 关 <c>Selectable</c>、拿到焦点立刻让出。</para>
        /// </summary>
        public bool Focusable
        {
            get { return _focusable; }
            set
            {
                if (_focusable == value) { return; }
                _focusable = value;
                SetStyle(ControlStyles.Selectable, value);
                TabStop = value;
                if (!value && Focused && Parent != null)
                {
                    Parent.SelectNextControl(this, true, true, true, true);
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 建句柄时 WinForms 会把 Selectable 重新打开，必须在那之后再关一次
            if (!_focusable) { SetStyle(ControlStyles.Selectable, false); }
        }

        public ModernComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable, true);
            TabStop = true;
            Font = Fonts.Body;
            Height = Metrics.Px(Metrics.InputHeight);
            Cursor = Cursors.Hand;
            BackColor = ThemeManager.Current.ControlBg;

            _itemCollection = new ItemCollection(_items, OnItemsChanged);

            _listBox = new DropDownListBox(_items);
            _listBox.ItemChosen += index => { SelectedIndex = index; CloseDropDown(); Invalidate(); };

            ToolStripControlHost host = new ToolStripControlHost(_listBox)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
            };
            _dropDown = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                AutoSize = false,
                DropShadowEnabled = true,
            };
            _dropDown.Items.Add(host);
            _dropDown.Closed += (s, e) => { _open = false; Invalidate(); };
            _host = host;
        }

        private readonly ToolStripControlHost _host;

        /// <summary>选项集合。</summary>
        public ItemCollection Items => _itemCollection;

        /// <summary>当前选中项索引，-1 表示未选中。</summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int clamped = value < 0 || value >= _items.Count ? -1 : value;
                if (clamped == _selectedIndex) { return; }
                _selectedIndex = clamped;
                _listBox.SelectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>当前选中项。</summary>
        public object SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

        /// <summary>当前选中项的文本。</summary>
        public override string Text
        {
            get { return SelectedItem?.ToString() ?? string.Empty; }
            set
            {
                int idx = _items.FindIndex(o => string.Equals(o?.ToString(), value, StringComparison.Ordinal));
                SelectedIndex = idx;
            }
        }

        /// <summary>下拉列表最多同时显示的项数。</summary>
        public int MaxDropDownItems { get; set; } = 10;

        public event EventHandler SelectedIndexChanged;

        private void OnItemsChanged()
        {
            if (_selectedIndex >= _items.Count) { SelectedIndex = -1; }
            Invalidate();
        }

        // —— 交互 ——

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
            // 不可获焦点时把焦点让出去。注意 OnMouseDown 里会主动 Focus()，
            // 和 ButtonBase 一样绕过了 Selectable，所以必须在这里拦一道。
            if (_focusable || _yieldingFocus) { return; }
            _yieldingFocus = true;
            try
            {
                Control c = Parent;
                if (c == null || !c.SelectNextControl(this, true, true, true, true)) { c?.Focus(); }
            }
            finally { _yieldingFocus = false; }
        }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            ToggleDropDown();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
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
                case Keys.Down:
                    e.Handled = true;
                    if (_open) { _listBox.MoveSelection(1); }
                    else if (_selectedIndex < _items.Count - 1) { SelectedIndex = _selectedIndex + 1; }
                    break;
                case Keys.Up:
                    e.Handled = true;
                    if (_open) { _listBox.MoveSelection(-1); }
                    else if (_selectedIndex > 0) { SelectedIndex = _selectedIndex - 1; }
                    break;
                case Keys.Space:
                case Keys.Enter:
                    if (!_open)
                    {
                        e.Handled = true;
                        OpenDropDown();
                    }
                    break;
                case Keys.Escape:
                    if (_open)
                    {
                        e.Handled = true;
                        CloseDropDown();
                    }
                    break;
            }
        }

        private void ToggleDropDown()
        {
            if (_open) { CloseDropDown(); }
            else { OpenDropDown(); }
        }

        private void OpenDropDown()
        {
            if (_items.Count == 0) { return; }

            int rowHeight = Metrics.Px(Metrics.GridRowHeight);
            int visible = Math.Min(_items.Count, MaxDropDownItems);
            int listHeight = visible * rowHeight;
            int width = Math.Max(Width, Metrics.Px(120));
            bool scroll = _items.Count > MaxDropDownItems;

            _listBox.RowHeight = rowHeight;
            _listBox.ShowScroll = scroll;
            _listBox.SelectedIndex = _selectedIndex;
            _listBox.Width = width;
            _listBox.Height = listHeight;

            _host.Size = new Size(width, listHeight);
            _dropDown.Size = new Size(width, listHeight);

            _open = true;
            _dropDown.Show(this, new Point(0, Height));
            _listBox.Focus();
            Invalidate();
        }

        private void CloseDropDown()
        {
            if (_open) { _dropDown.Close(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Palette p = ThemeManager.Current;
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(Drawing.ParentBackColor(this, p.CardBg))) { g.FillRectangle(back, ClientRectangle); }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            int radius = Metrics.Px(Metrics.SmallRadius);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color border = _hover || Focused || _open ? p.Accent : p.ControlBorder;
            Drawing.FillRoundedRect(g, r, radius, p.ControlBg, border, Metrics.Px(1));

            int pad = Metrics.Px(10);
            int chevronW = Metrics.Px(18);
            Rectangle textRect = new Rectangle(pad, 0, Math.Max(0, Width - pad - chevronW - Metrics.Px(6)), Height);
            string display = SelectedItem?.ToString() ?? string.Empty;
            Drawing.DrawText(g, display, Font, p.Text, textRect, ContentAlignment.MiddleLeft, clearType: false);

            DrawChevron(g, new Rectangle(Width - pad - chevronW + Metrics.Px(5), Height / 2, chevronW, Metrics.Px(6)), p.TextMuted);
        }

        private void DrawChevron(Graphics g, Rectangle r, Color color)
        {
            using (Pen pen = new Pen(color, Metrics.Px(2)))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                float midX = r.Left + r.Width / 2f;
                float midY = r.Top + r.Height / 2f;
                g.DrawLines(pen, new[]
                {
                    new PointF(midX - r.Width * 0.32f, midY - r.Height * 0.18f),
                    new PointF(midX, midY + r.Height * 0.30f),
                    new PointF(midX + r.Width * 0.32f, midY - r.Height * 0.18f),
                });
            }
        }

        /// <summary>选项集合。</summary>
        public sealed class ItemCollection : IList
        {
            private readonly List<object> _items;
            private readonly Action _changed;

            internal ItemCollection(List<object> items, Action changed)
            {
                _items = items;
                _changed = changed;
            }

            public int Count => _items.Count;
            public bool IsReadOnly => false;
            public bool IsFixedSize => false;
            public object SyncRoot => this;
            public bool IsSynchronized => false;

            public object this[int index]
            {
                get { return _items[index]; }
                set { _items[index] = value; _changed(); }
            }

            public int Add(object value) { _items.Add(value); _changed(); return _items.Count - 1; }

            public void AddRange(object[] values)
            {
                if (values == null) { return; }
                _items.AddRange(values);
                _changed();
            }

            public void AddRange(IEnumerable<object> values)
            {
                if (values == null) { return; }
                _items.AddRange(values);
                _changed();
            }

            public void Clear() { _items.Clear(); _changed(); }
            public bool Contains(object value) => _items.Contains(value);
            public int IndexOf(object value) => _items.IndexOf(value);
            public void Insert(int index, object value) { _items.Insert(index, value); _changed(); }
            public void Remove(object value) { _items.Remove(value); _changed(); }
            public void RemoveAt(int index) { _items.RemoveAt(index); _changed(); }
            public void CopyTo(Array array, int index) { ((ICollection)_items).CopyTo(array, index); }
            public IEnumerator GetEnumerator() => _items.GetEnumerator();
        }
    }
}
