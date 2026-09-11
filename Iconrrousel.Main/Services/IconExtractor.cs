using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Iconrrousel.Main
{
    public static class IconExtractor
    {
        // Constantes para el tamaño Jumbo (256x256)
        private const int SHIL_JUMBO = 0x4;
        private const uint SHGFI_SYSICONINDEX = 0x4000;

        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO pszFi, uint cbFileInfo, uint uFlags);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IImageList
        {
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            int SetOverlayImage(int iImage, int iOverlay);
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            int AddMasked(IntPtr hbmImage, uint crMask, ref int pi);
            int Draw(IntPtr pimldp); // <-- CAMBIO AQUÍ: Usamos IntPtr en lugar de IMAGELISTDRAWPARAMS
            int Remove(int i);
            int GetIcon(int i, int flags, out IntPtr picon); // <-- Esto ahora quedará en la posición 8
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        // Respaldo para cuando la lista de iconos "jumbo" del sistema (SHIL_JUMBO)
        // devuelve un bitmap completamente transparente para un archivo puntual -
        // se vio con accesos directos .url con icono personalizado (ej. juegos de
        // Steam) que el Explorador de Windows muestra bien pero que esa lista
        // compartida entrega en blanco. IShellItemImageFactory es la misma API que
        // usa el Explorador para generar sus miniaturas/iconos grandes, y no
        // depende de ningun paquete NuGet externo.
        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItemImageFactory
        {
            void GetImage(SIZE size, int flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int size) { cx = size; cy = size; }
        }

        private const int SIIGBF_ICONONLY = 0x4;
        private static readonly Guid IID_IShellItemImageFactory = new Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B");

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            string pszPath,
            IntPtr pbc,
            ref Guid riid,
            out IShellItemImageFactory ppv);

        public static ImageSource GetJumboIcon(string filePath)
        {
            if (_iconCache.TryGetValue(filePath, out ImageSource cachedIcon))
            {
                return cachedIcon;
            }

            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_SYSICONINDEX);

            Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            IImageList iml = null;
            IntPtr hIcon = IntPtr.Zero;

            try
            {
                SHGetImageList(SHIL_JUMBO, ref iidImageList, out iml);
                iml.GetIcon(shinfo.iIcon, 1, out hIcon); // ILD_TRANSPARENT

                using (var icon = Icon.FromHandle(hIcon))
                using (var bitmap = icon.ToBitmap())
                using (var fallbackBitmap = IsFullyTransparent(bitmap) ? TryGetShellItemIcon(filePath, 256) : null)
                using (var croppedBmp = CropTransparent(fallbackBitmap ?? bitmap))
                {
                    IntPtr hBitmap = croppedBmp.GetHbitmap();
                    try
                    {
                        var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();

                        _iconCache[filePath] = source;
                        return source;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            finally
            {
                if (hIcon != IntPtr.Zero)
                    DestroyIcon(hIcon);

                if (iml != null)
                    Marshal.ReleaseComObject(iml);
            }
        }

        private static bool IsFullyTransparent(Bitmap bmp) => GetBoundingBox(bmp) == Rectangle.Empty;

        private static Bitmap TryGetShellItemIcon(string filePath, int size)
        {
            try
            {
                var riid = IID_IShellItemImageFactory;
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref riid, out var factory);
                if (factory == null)
                    return null;

                factory.GetImage(new SIZE(size), SIIGBF_ICONONLY, out IntPtr hBitmap);
                if (hBitmap == IntPtr.Zero)
                    return null;

                try
                {
                    using (var raw = Image.FromHbitmap(hBitmap))
                    {
                        return new Bitmap(raw); // copia propia, independiente del HBITMAP nativo
                    }
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCache()
        {
            _iconCache.Clear();
        }

        public static void RemoveFromCache(string filePath)
        {
            _iconCache.Remove(filePath);
        }

        private static Bitmap CropTransparent(Bitmap bmp)
        {
            var rect = GetBoundingBox(bmp);
            return rect == Rectangle.Empty
                ? bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), bmp.PixelFormat)
                : bmp.Clone(rect, bmp.PixelFormat);
        }

        private static Rectangle GetBoundingBox(Bitmap bmp)
        {
            // Bloqueamos los bits para analizar la transparencia (Alpha)
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int[] rgbValues = new int[data.Width * data.Height];
            Marshal.Copy(data.Scan0, rgbValues, 0, rgbValues.Length);
            bmp.UnlockBits(data);

            int left = data.Width, top = data.Height, right = 0, bottom = 0;
            bool hasAlpha = false;

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    // Verificamos si el píxel no es totalmente transparente (Alpha > 0)
                    if (((rgbValues[y * data.Width + x] >> 24) & 0xff) > 0)
                    {
                        if (x < left) left = x;
                        if (x > right) right = x;
                        if (y < top) top = y;
                        if (y > bottom) bottom = y;
                        hasAlpha = true;
                    }
                }
            }
            return hasAlpha ? new Rectangle(left, top, right - left + 1, bottom - top + 1) : Rectangle.Empty;
        }
    }
}