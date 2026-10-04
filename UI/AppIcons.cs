using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace KeyboardChatterBlocker.UI
{
    /// <summary>从嵌入资源加载图标。</summary>
    internal static class AppIcons
    {
        private const string ResourceName = "KeyboardChatterBlocker.Assets.keyboard.ico";

        private static Icon _cached;

        /// <summary>应用图标（原始尺寸）。</summary>
        public static Icon App
        {
            get
            {
                if (_cached == null)
                {
                    using (Stream s = typeof(AppIcons).Assembly.GetManifestResourceStream(ResourceName))
                    {
                        if (s != null)
                        {
                            _cached = new Icon(s);
                        }
                    }
                }
                return _cached;
            }
        }

        /// <summary>托盘/标题栏用小图标，尺寸随 DPI 变化。</summary>
        public static Icon Small()
        {
            Size size = SystemInformation.SmallIconSize;
            Icon icon = App;
            if (icon == null) { return SystemIcons.Application; }
            try
            {
                using (Icon sized = new Icon(icon, size))
                {
                    return (Icon)sized.Clone();
                }
            }
            catch
            {
                return icon;
            }
        }
    }
}
