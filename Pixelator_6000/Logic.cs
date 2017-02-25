using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace Pixelator_6000
{
    public enum Orientation { up, down, right, left };
    class Logic
    {
        public delegate EventHandler RedrawEvent(object sender, RedrawEventArgs e);
        public event RedrawEvent RedrawImageAfter;

        public void PixelsortBySaturation(bool bright, Orientation orientation, float limit, Bitmap original)
        {
            using (Bitmap bmp = (Bitmap)original.Clone())
            {
            int PixelSize = 4;
            bool sorting = false;
            Color recent = new Color();
            Color current = new Color();

            BitmapData bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                bmp.PixelFormat);
            unsafe
            {
                for (int y = 0; y < bmd.Height; y++)
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);

                    //iterate horizontally, line by line
                    for (int x = 0; x < bmd.Width; x++)
                    {
                        //initialize values
                        current = GetPixel(row, x, PixelSize);
                        
                        //sort this pixel?
                        if (bright)
                        {
                            sorting = current.GetBrightness() > limit;
                        }
                        else
                        {
                            sorting = current.GetBrightness() < limit;
                        }

                        //if so, sort!
                        if (sorting )
                        {
                            SetPixel(row, x, PixelSize, recent);
                        }
                        else
                        {
                            recent = current;
                        }                        
                    }
                }
            }
            bmp.UnlockBits(bmd);
            
            RedrawImageAfter(this, new RedrawEventArgs(bmp));
            }

        }

        private unsafe void SetPixel(byte* row, int x, int pixelSize, Color setThis)
        {
            row[x * pixelSize] = setThis.B;   //Blue  0-255
            row[x * pixelSize + 1] = setThis.G; //Green 0-255
            row[x * pixelSize + 2] = setThis.R;   //Red   0-255
            row[x * pixelSize + 3] = setThis.A;  //Alpha 0-255
        }

        private unsafe Color GetPixel(byte* row, int x, int pixelSize)
        {
            return Color.FromArgb(
                row[x * pixelSize + 3],   //Alpha 0-255
                row[x * pixelSize + 2],   //Red   0-255
                row[x * pixelSize + 1],   //Green 0-255
                row[x * pixelSize]        //Blue  0-255
                );
        }
    }

    
}

/*----------------------------------------------
 *                   TEMPLATE                           
 *----------------------------------------------
            
    Bitmap bmp = (Bitmap)original.Clone();
    BitmapData bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                        System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                        bmp.PixelFormat);

    int PixelSize = 4;

    unsafe
    {
        for (int y = 0; y < bmd.Height; y++)
        {
            byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);

            //iterate horizontally, line by line
            for (int x = 0; x < bmd.Width; x++)
            {
                        
                row[x * PixelSize] = 0;   //Blue  0-255
                row[x * PixelSize + 1] = 255; //Green 0-255
                row[x * PixelSize + 2] = 0;   //Red   0-255
                row[x * PixelSize + 3] = 50;  //Alpha 0-255
                        
            }
        }
    }

    bmp.UnlockBits(bmd);

     */
