using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace AdminProgram.Helpers
{
    public static class BitmapImageHelper
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        // {
        //     var hBitmap = bitmap.GetHbitmap();
        //     BitmapImage retval;
        //
        //     try
        //     {
        //         retval = (BitmapImage)Imaging.CreateBitmapSourceFromHBitmap(
        //             hBitmap,
        //             IntPtr.Zero,
        //             Int32Rect.Empty,
        //             BitmapSizeOptions.FromEmptyOptions());
        //     }
        //     finally
        //     {
        //         DeleteObject(hBitmap);
        //     }
        //
        //     return retval;
        // }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Jpeg);
            ms.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        public static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using var outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }
    }
}