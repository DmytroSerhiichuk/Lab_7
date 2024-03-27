using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Lab_7_Client.Utils
{
        internal class ScreenShot
        {
            public static Bitmap GetWindow(IntPtr hWnd, int width, int height)
            {
                var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr hdcBitmap = g.GetHdc();
                    PrintWindow(hWnd, hdcBitmap, 0);
                    g.ReleaseHdc(hdcBitmap);

                    var rBmp = new Bitmap(1280, 720);

                    using (var g2 = Graphics.FromImage(rBmp))
                    {
                        g2.DrawImage(bmp, new Rectangle(0, 0, 1280, 720));
                    }

                    return rBmp;
                }
            }

            public static Bitmap GetFullScreen(int width, int height)
            {
                var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, new Size(width, height));

                    var rBmp = new Bitmap(1280, 720);

                    using (var g2 = Graphics.FromImage(rBmp))
                    {
                        g2.DrawImage(bmp, new Rectangle(0, 0, 1280, 720));
                    }

                    return rBmp;
                }
            }



            [DllImport("user32.dll")]
            private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

            internal static object GetFullScreen(double actualWidth, double actualHeight)
            {
                throw new NotImplementedException();
            }
        }
}
