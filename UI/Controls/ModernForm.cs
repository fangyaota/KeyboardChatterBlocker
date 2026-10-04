using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardChatterBlocker.UI.Theme;

namespace KeyboardChatterBlocker.UI.Controls
{
    /// <summary>
    /// 无边框现代窗口基类：系统投影 + Win11 圆角 + 边缘缩放 + 标题栏拖动。
    /// </summary>
    public class ModernForm : Form
    {
        private const int WM_NCHITTEST = 0x0084;

        public ModernForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
            BackColor = ThemeManager.Current.WindowBg;
            Font = Fonts.Body;
            // 关闭框架自动缩放：PerMonitorV2 下 AutoScaleDimensions 会被框架改写成当前 DPI，
            // 缩放因子因此恒为 1（控件不缩放、文字却按高 DPI 渲染）。改由 Metrics.Px 统一换算。
            AutoScaleMode = AutoScaleMode.None;
            MinimumSize = new Size(Metrics.Px(1020), Metrics.Px(600));
            Size = new Size(Metrics.Px(1180), Metrics.Px(700));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // CS_DROPSHADOW 是「类样式」，必须写进 ClassStyle；写进 Style 无效
                cp.ClassStyle |= NativeMethods.CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDwmChrome();
        }

        /// <summary>应用 Win11 圆角并消除 DWM 细白边。</summary>
        protected void ApplyDwmChrome()
        {
            if (!NativeMethods.IsWindows11OrGreater()) { return; }
            try
            {
                int pref = NativeMethods.DWMWCP_ROUND;
                NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
                int none = NativeMethods.DWMWA_COLOR_NONE;
                NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_BORDER_COLOR, ref none, sizeof(int));
            }
            catch
            {
                // DWM 不可用时退回直角，不影响功能
            }
        }

        /// <summary>以标题栏方式拖动窗口。</summary>
        public void DragWindow()
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
        }

        // —— 边缘缩放 ——
        // 无边框窗口的客户区被 Dock=Fill 的子控件铺满，窗体本身收不到 WM_NCHITTEST，
        // 因此在控件树各节点上挂鼠标事件，命中边缘时转交给系统的非客户区拖动逻辑。

        private Control _resizeRoot;

        /// <summary>在指定控件树范围内启用边缘缩放。</summary>
        public void EnableEdgeResize(Control root)
        {
            _resizeRoot = root;
            AttachResizeHandlers(root);
            root.ControlAdded += (s, e) => AttachResizeHandlers(e.Control);
        }

        private void AttachResizeHandlers(Control c)
        {
            c.MouseMove += ResizeHost_MouseMove;
            c.MouseDown += ResizeHost_MouseDown;
            c.MouseLeave += (s, e) => { if (!_resizing) { Cursor = Cursors.Default; } };
            foreach (Control child in c.Controls)
            {
                AttachResizeHandlers(child);
            }
        }

        private bool _resizing;

        private int HitTestEdge(Control c, Point p)
        {
            if (_resizeRoot == null) { return 0; }
            Point formPt = PointToClient(c.PointToScreen(p));
            if (WindowState != FormWindowState.Normal) { return 0; }

            int grip = Metrics.Px(Metrics.ResizeGrip);
            bool left = formPt.X <= grip;
            bool right = formPt.X >= ClientSize.Width - grip;
            bool top = formPt.Y <= grip;
            bool bottom = formPt.Y >= ClientSize.Height - grip;

            if (top && left) { return NativeMethods.HTTOPLEFT; }
            if (top && right) { return NativeMethods.HTTOPRIGHT; }
            if (bottom && left) { return NativeMethods.HTBOTTOMLEFT; }
            if (bottom && right) { return NativeMethods.HTBOTTOMRIGHT; }
            if (left) { return NativeMethods.HTLEFT; }
            if (right) { return NativeMethods.HTRIGHT; }
            if (top) { return NativeMethods.HTTOP; }
            if (bottom) { return NativeMethods.HTBOTTOM; }
            return 0;
        }

        private void ResizeHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (_resizing || sender is not Control c) { return; }
            int edge = HitTestEdge(c, e.Location);
            Cursor = edge switch
            {
                NativeMethods.HTLEFT or NativeMethods.HTRIGHT => Cursors.SizeWE,
                NativeMethods.HTTOP or NativeMethods.HTBOTTOM => Cursors.SizeNS,
                NativeMethods.HTTOPLEFT or NativeMethods.HTBOTTOMRIGHT => Cursors.SizeNWSE,
                NativeMethods.HTTOPRIGHT or NativeMethods.HTBOTTOMLEFT => Cursors.SizeNESW,
                _ => Cursors.Default,
            };
        }

        private void ResizeHost_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is not Control c || e.Button != MouseButtons.Left) { return; }
            int edge = HitTestEdge(c, e.Location);
            if (edge == 0) { return; }
            _resizing = true;
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)edge, IntPtr.Zero);
            _resizing = false;
        }

        protected override void WndProc(ref Message m)
        {
            // 窗体自身的空白区（未被控件覆盖）也走同一套命中逻辑
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == 1 /*HTCLIENT*/)
                {
                    int x = unchecked((short)(long)m.LParam);
                    int y = unchecked((short)((long)m.LParam >> 16));
                    Point pt = PointToClient(new Point(x, y));
                    int grip = Metrics.Px(Metrics.ResizeGrip);
                    if (pt.X <= grip) { m.Result = (IntPtr)NativeMethods.HTLEFT; return; }
                    if (pt.X >= ClientSize.Width - grip) { m.Result = (IntPtr)NativeMethods.HTRIGHT; return; }
                    if (pt.Y <= grip) { m.Result = (IntPtr)NativeMethods.HTTOP; return; }
                    if (pt.Y >= ClientSize.Height - grip) { m.Result = (IntPtr)NativeMethods.HTBOTTOM; return; }
                }
                return;
            }
            base.WndProc(ref m);
        }
    }
}
