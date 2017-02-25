using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Pixelator_6000
{
    public static class BitmapConverter
    {
        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource Bitmap2BitmapSource(Bitmap bitmap)
        {
            using (bitmap)
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                BitmapSource retval = Imaging.CreateBitmapSourceFromHBitmap(
                                 hBitmap,
                                 IntPtr.Zero,
                                 Int32Rect.Empty,
                                 BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(hBitmap);
                return retval;
            }
        }

        public static Bitmap BitmapSource2Bitmap(BitmapSource bmpSource)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return new Bitmap(ms);
            }                
        }        
    }
}
