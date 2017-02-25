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
    public enum Orientation { up, right, down, left };
    class Logic
    {
        public delegate EventHandler RedrawEvent(object sender, RedrawEventArgs e);
        public event RedrawEvent RedrawImageAfter;

        public void PixelsortByBrightness(bool bright, Orientation pixelsortDirection, float limit, Bitmap orig)
        {
            //base image orientation = up
            //base pixel sort orientation = right
            //the orientation of the image is always -90° to the orientation of the desired pixelsort
            //let's reflect that:
            Orientation imageDirection = Orientation.up;
            switch (pixelsortDirection)
            {
                case Orientation.up:
                    {
                        imageDirection = Orientation.left;
                        break;
                    }
                case Orientation.right:
                    {
                        imageDirection = Orientation.up;
                        break;
                    }
                case Orientation.down:
                    {
                        imageDirection = Orientation.right;
                        break;
                    }
                case Orientation.left:
                    {
                        imageDirection = Orientation.down;
                        break;
                    }
            }
            //and rotate the whole image so that the sorting algorithm can remain the same, it's complicated enough as it is
            orig = Rotate(orig, imageDirection, reverse: false);


            BitmapChest chest = new BitmapChest(orig);
            chest.LockBits();
            bool sorting = false;

            Color current = new Color();
            Color recent = new Color();
            for (int y = 0; y < chest.Height; y++)
            {
                //iterate horizontally, line by line
                for (int x = 0; x < chest.Width; x++)
                {

                    //initialize values
                    current = chest.GetPixel(x, y);

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
                    if (sorting)
                    {
                        chest.SetPixel(x, y, recent);
                    }
                    else
                    {
                        recent = current;
                    }
                }

            }
            chest.UnlockBits();
            //rotate the whole image back before you return it
            orig = Rotate(orig, imageDirection, reverse: true);
            RedrawImageAfter(this, new RedrawEventArgs(orig));

        }

        public void Prism(int rOffsetX, int rOffsetY, int gOffsetX, int gOffsetY, int bOffsetX, int bOffsetY, Bitmap orig)
        {
            Bitmap toDraw = new Bitmap(orig, orig.Size);
            var chest = new BitmapChest((Bitmap)orig);
            var drawChest = new BitmapChest(toDraw);
            //create 3 color maps and assemble them back together with offset
            chest.LockBits();
            drawChest.LockBits();
            var chestRectangle = new Rectangle(0, 0, chest.Width, chest.Height);
            //fill the three color maps with their respective colors
            unsafe
            {
                //for the height of this image
                for (int y = 0; y < chest.Height; y++)
                {
                    //iterate horizontally, line by line
                    for (int x = 0; x < chest.Width; x++)
                    {
                        Color origColor = chest.GetPixel(x, y);
                        Color newColor = new Color();
                        //find the line in memory

                        //if there is such an X and Y...
                        if ( chestRectangle.Contains(new Point(x - rOffsetX, y - rOffsetY)) &&
                            chestRectangle.Contains(new Point(x - gOffsetX, y - gOffsetY)) &&
                            chestRectangle.Contains(new Point(x - bOffsetX, y - bOffsetY)))
                        /* y + rOffsetY >= 0 && y + rOffsetY < chest.Height &&
                         y + gOffsetY >= 0 && y + gOffsetY < chest.Height &&
                         y + bOffsetY >= 0 && y + bOffsetY < chest.Height &&

                         x + rOffsetX >= 0 &&    x + rOffsetX < chest.Width &&
                         x + gOffsetX >= 0 &&    x + gOffsetX < chest.Width &&
                         x + bOffsetX >= 0 &&    x + bOffsetX < chest.Width*/
                        {
                            //...adjust for the offset...
                            newColor = Color.FromArgb(origColor.A,
                            chest.GetPixel(x - rOffsetX, y - rOffsetY).R,
                            chest.GetPixel(x - gOffsetX, y - gOffsetY).G,
                            chest.GetPixel(x - bOffsetX, y - bOffsetY).B);
                            //...and paint it back
                            drawChest.SetPixel(x, y, newColor);
                        }
                    }
                }                
                chest.UnlockBits();
                drawChest.UnlockBits();
                RedrawImageAfter(this, new RedrawEventArgs(toDraw));
            }
        }

        private Bitmap Rotate(Bitmap bmp, Orientation orientation, bool reverse)
        {
            //angles are considered counter-clockwise
            if (orientation != Orientation.up)
            {
                switch (orientation)
                {
                    case Orientation.right:
                        {
                            if (reverse)
                            {
                                bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            }
                            if (!reverse)
                            {
                                bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            }
                            break;
                        }
                    case Orientation.down:
                        {
                            bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        }
                    case Orientation.left:
                        {
                            if (reverse)
                            {
                                bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            }
                            if (!reverse)
                            {
                                bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            }
                            break;
                        }
                }

            }
            return bmp;
        }
    }
}
    

        /*
        private unsafe void SetPixel(byte* row, int x, Color setThis)
        {
            int pixelSize = 4;
            row[x * pixelSize] = setThis.B;         //Blue  0-255
            row[x * pixelSize + 1] = setThis.G;     //Green 0-255
            row[x * pixelSize + 2] = setThis.R;     //Red   0-255
            row[x * pixelSize + 3] = setThis.A;     //Alpha 0-255
        }

        /// <summary>
        /// This fully trusts the caller. Do not ask for addresses that are not there!
        /// </summary>
        /// <param name="row"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private unsafe Color GetPixel(byte* row, int x, int width, int depth)
        {
            int blue = 0;
            int green = 0;
            int red = 0;
            int alpha = 0;
            
            blue = row[x * depth];          //Blue  0-255
            green = row[x * depth + 1];     //Green  0-255
            red = row[x * depth + 2];       //Red   0-255
            alpha = row[x * depth + 3];     //Alpha 0-255

            
            if(x * pixelSize + 3 < width)
            { 
                blue = row[x * pixelSize];          //Blue  0-255
                green = row[x * pixelSize + 1];     //Green  0-255
                red = row[x * pixelSize + 2];       //Red   0-255
                alpha = row[x * pixelSize + 3];     //Alpha 0-255
            }            
            else
            {
                blue = row[(x * pixelSize) % (width)];
                green = row[(x * pixelSize + 1) % (width)];
                red = row[(x * pixelSize + 2) % (width)];
                alpha = row[(x * pixelSize + 3) % width];
            }

            Color result = Color.FromArgb(blue, green, red);

            return result;
        }/*
    }

    
}

/*----------------------------------------------
 *                   TEMPLATE                           
 *----------------------------------------------
            
    // get Bitmap bmp as parameter
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
