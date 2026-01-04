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

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO pszFi, uint cbFileInfo, uint uFlags);

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


        public static ImageSource GetJumboIcon(string filePath)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            // Obtener el índice del icono en el sistema
            SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_SYSICONINDEX);

            Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            IImageList iml;

            // Cargar la lista de imágenes JUMBO
            SHGetImageList(SHIL_JUMBO, ref iidImageList, out iml);

            IntPtr hIcon = IntPtr.Zero;
            iml.GetIcon(shinfo.iIcon, 1, out hIcon); // 1 = ILD_TRANSPARENT

            Bitmap bitmap = Icon.FromHandle(hIcon).ToBitmap();

            using (Bitmap originalBmp = Icon.FromHandle(hIcon).ToBitmap())
            {
                using (Bitmap croppedBmp = CropTransparent(originalBmp)) // <-- AÑADE ESTO
                {
                    IntPtr hBitmap = croppedBmp.GetHbitmap();
                    try
                    {
                        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                    finally
                    {
                        // ¡Importante! Liberar memoria GDI para evitar fugas (Memory Leaks)
                        DeleteObject(hBitmap);
                    }
                }
            }
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);     

        private static Bitmap CropTransparent(Bitmap bmp)
        {
            var rect = GetBoundingBox(bmp);
            if (rect == Rectangle.Empty) return bmp;
            return bmp.Clone(rect, bmp.PixelFormat);
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