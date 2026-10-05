using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 换肤后的 <see cref="DataGridView"/>。
    /// 刻意继承原生控件而非自绘列表：原 UI 逻辑重度依赖
    /// <c>Rows.Add</c> / <c>FirstDisplayedScrollingRowIndex</c> / <c>DisplayedRowCount(true)</c> /
    /// <c>SelectedCells</c> / <c>ClearSelection</c> 等语义，自绘列表要重写这些，风险高、收益低。
    /// </summary>
    public class ModernDataGridView : DataGridView
    {
        private bool _focusable = true;
        private bool _yieldingFocus;

        /// <summary>
        /// 是否接受焦点，默认 <c>true</c>。
        /// <para>
        /// 设为 <c>false</c> 后表格只能用鼠标操作（点选、双击、滚轮），
        /// 键盘完全不会落进来：没有焦点 → 方向键不改选中、空格/回车不触发按钮单元格、
        /// 打字也不会触发「首字母跳行」。
        /// </para>
        /// <para>
        /// 「抖动日志」「统计」两页纯展示，必须屏蔽 —— 这是个键盘工具，
        /// 用户敲键盘时不该把界面点着。而「按键配置」页要用 Delete 删键，保持可获焦点。
        /// </para>
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

        /// <summary>
        /// 不可获焦点时，一旦拿到焦点就立刻让给下一个控件。
        /// 光关 <see cref="ControlStyles.Selectable"/> 挡不住鼠标点击带来的焦点，
        /// 必须在焦点落下的瞬间把它交出去。
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (_focusable || _yieldingFocus) { return; }
            _yieldingFocus = true;
            try
            {
                Control c = Parent;
                if (c == null || !c.SelectNextControl(this, true, true, true, true)) { c?.Focus(); }
            }
            finally { _yieldingFocus = false; }
        }

        public ModernDataGridView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;

            // 不设的话，表头 BackColor / ForeColor 完全无效（永远跟随系统视觉样式）
            EnableHeadersVisualStyles = false;

            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;
            MultiSelect = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ScrollBars = ScrollBars.Vertical;
            Font = Fonts.Body;
            BackgroundColor = ThemeManager.Current.GridBg;
            ApplyTheme();
        }

        /// <summary>
        /// 行被增删后，重新套用用户当前选择的排序。
        /// <para>
        /// DataGridView 的排序状态记在列的 <c>HeaderCell.SortGlyphDirection</c> 上，
        /// <c>Rows.Clear()</c> 不会清掉它 —— 但新加入的行也不会自动按它排列。
        /// 因此每次重建或追加行之后都必须显式重排，
        /// 否则用户点击列头选的排序会在下一次刷新时被悄悄重置掉。
        /// </para>
        /// </summary>
        public void ReapplySort()
        {
            foreach (DataGridViewColumn column in Columns)
            {
                SortOrder order = column.HeaderCell.SortGlyphDirection;
                if (order == SortOrder.None)
                {
                    continue;
                }
                Sort(column, order == SortOrder.Ascending
                    ? ListSortDirection.Ascending
                    : ListSortDirection.Descending);
                return;
            }
        }

        /// <summary>按当前主题刷新配色。主题变更时由窗体调用。</summary>
        public void ApplyTheme()
        {
            Palette p = ThemeManager.Current;

            BackgroundColor = p.GridBg;
            GridColor = p.GridLine;

            ColumnHeadersHeight = Metrics.Px(Metrics.GridHeaderHeight);
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = p.GridHeaderBg,
                ForeColor = p.TextMuted,
                SelectionBackColor = p.GridHeaderBg,
                SelectionForeColor = p.TextMuted,
                Font = Fonts.Body,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(Metrics.Px(8), 0, 0, 0),
                WrapMode = DataGridViewTriState.False,
            };

            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = p.GridBg,
                ForeColor = p.Text,
                SelectionBackColor = p.GridSelBg,
                SelectionForeColor = p.GridSelText,
                Font = Fonts.Body,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(Metrics.Px(8), 0, 0, 0),
                WrapMode = DataGridViewTriState.False,
            };

            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = p.IsDark ? p.GridBg : p.GridAltBg,
                ForeColor = p.Text,
                SelectionBackColor = p.GridSelBg,
                SelectionForeColor = p.GridSelText,
                Font = Fonts.Body,
                Padding = new Padding(Metrics.Px(8), 0, 0, 0),
                WrapMode = DataGridViewTriState.False,
            };

            RowTemplate.Height = Metrics.Px(Metrics.GridRowHeight);
        }
    }
}
