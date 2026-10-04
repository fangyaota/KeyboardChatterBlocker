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
