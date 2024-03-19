using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Lab_7_Client.Utils
{
    internal class ScreenRecorder
    {
        public static Bitmap GetWindow(IntPtr hWnd, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bmp))
            {
                IntPtr hdcBitmap = g.GetHdc();
                PrintWindow(hWnd, hdcBitmap, 0);
                g.ReleaseHdc(hdcBitmap);

                return bmp;
            }
        }

        public static Bitmap GetFullScreen(int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, new Size(width, height));

                return bmp;
            }
        }



        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    }
}
