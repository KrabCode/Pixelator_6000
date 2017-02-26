using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Pixelator_6000
{
    public enum Orientation { up, right, down, left };
    public enum Effect { pixelsort, prism};
    class Logic
    {
        public delegate EventHandler RedrawEvent(object sender, RedrawEventArgs e);
        public event RedrawEvent RedrawImageAfter;
        
        public void PixelsortByBrightness(bool bright, Orientation pixelsortDirection, float limit, Bitmap orig)
        {
            //base image orientation = up
            //base pixel sort orientation = right (the cycle is: for each line, iterate the columns)
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

            //for each line
            for (int y = 0; y < chest.Height; y++)
            {
                //iterate through the columns horizontally
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

        /// <summary>
        /// Applies the Prism effect and updates imageAfter with the result
        /// </summary>
        /// <param name="rOffsetX"></param>
        /// <param name="rOffsetY"></param>
        /// <param name="gOffsetX"></param>
        /// <param name="gOffsetY"></param>
        /// <param name="bOffsetX"></param>
        /// <param name="bOffsetY"></param>
        /// <param name="orig">Shouldn't be null</param>
        public void Prism(int rOffsetX, int rOffsetY, int gOffsetX, int gOffsetY, int bOffsetX, int bOffsetY, Bitmap orig)
        {
            if(orig != null)
            {
                Bitmap toDraw = new Bitmap(orig, orig.Size);
                var origChest = new BitmapChest((Bitmap)orig);
                var drawChest = new BitmapChest(toDraw);

                //before modifying the values, it's essential to lock the bits
                origChest.LockBits();
                drawChest.LockBits();

                var chestRectangle = new Rectangle(0, 0, origChest.Width, origChest.Height);
                //fill the three color maps with their respective colors
                unsafe
                {
                    //for the height of this image
                    for (int y = 0; y < origChest.Height; y++)
                    {
                        //iterate horizontally, line by line
                        for (int x = 0; x < origChest.Width; x++)
                        {
                            Color origColor = origChest.GetPixel(x, y);
                            Color newColor = new Color();

                            //if there is such an X and Y...
                            if (chestRectangle.Contains(new System.Drawing.Point(x + rOffsetX, y + rOffsetY)) &&
                                chestRectangle.Contains(new System.Drawing.Point(x + gOffsetX, y + gOffsetY)) &&
                                chestRectangle.Contains(new System.Drawing.Point(x + bOffsetX, y + bOffsetY)))
                            {
                                //...adjust for the offset...
                                newColor = Color.FromArgb(origColor.A,                                
                                    origChest.GetPixel(x + rOffsetX, y + rOffsetY).R,
                                    origChest.GetPixel(x + gOffsetX, y + gOffsetY).G,
                                    origChest.GetPixel(x + bOffsetX, y + bOffsetY).B);

                                //...and paint it back
                                drawChest.SetPixel(x, y, newColor);
                            }
                        }
                    }
                    origChest.UnlockBits();
                    drawChest.UnlockBits();
                    RedrawImageAfter(this, new RedrawEventArgs(toDraw));
                }
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
    

        
