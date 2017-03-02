using SuperfastBlur;
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

    class Logic
    {
        //If you're crafting a new effect, observe the rules outlined in MainWindow.xaml.cs!
        //Namely: return the resulting bitmap using the event RedrawImageAfter...
        //...and pass your result to it as a constructor parameter of a new RedrawEventArgs
        //like so: RedrawImageAfter(this, new RedrawEventArgs(modifiedBitmap));
        public delegate EventHandler RedrawEvent(object sender, RedrawEventArgs e);
        public event RedrawEvent RedrawImageAfter;

        

        public void PixelsortByBrightness(bool bright, Orientation pixelsortDirection, float limit, Bitmap orig)
        {
            //in order to implement pixelsort in four directions I just rotate the image prior to the main cycle

            //base image orientation = up
            //^^^^
            //||||
            //||||

            //base pixel sort orientation = right (the cycle is: for each line, iterate the columns)
            //---->
            //---->
            //---->

            //the orientation of the image is always +90°(counter-clockwise) to the orientation of the desired pixelsort
            //let's reflect that:
            Orientation desiredImageDirection = Orientation.up;
            switch (pixelsortDirection)
            {
                case Orientation.up:
                    {
                        desiredImageDirection = Orientation.left;
                        break;
                    }
                case Orientation.right:
                    {
                        desiredImageDirection = Orientation.up;
                        break;
                    }
                case Orientation.down:
                    {
                        desiredImageDirection = Orientation.right;
                        break;
                    }
                case Orientation.left:
                    {
                        desiredImageDirection = Orientation.down;
                        break;
                    }
            }

            //and rotate the whole image so that the sorting algorithm can remain the same, it's complicated enough as it is
            orig = Rotate(orig, desiredImageDirection, reverse: false);

            //this chest boosts performance by orders of magnitude as opposed to Bitmap.getPixel(x, y) and Bitmap.setPixel(x, y, Color)
            //only thing to keep in mind is to lock it prior to reading or modifying it and unlock it afterwards.
            BitmapChest chest = new BitmapChest(orig);
            chest.LockBits();


            //Main pixelsorting cycle:
            bool sorting = false;
            Color current = new Color();
            Color recent = new Color();

            //for each line
            for (int y = 0; y < chest.Height; y++)
            {
                //iterate through the columns horizontally
                for (int x = 0; x < chest.Width; x++)
                {
                    //---->
                    //---->
                    //---->

                    //what's this pixel's color?
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
            //unlock the chest before using the image
            chest.UnlockBits();

            //rotate the whole image back before you return it
            orig = Rotate(orig, desiredImageDirection, reverse: true);

            //fire the event that redraws the modified image display on a UI thread in MainWindow.xaml.cs
            RedrawImageAfter(this, new RedrawEventArgs(orig));
        }

        public void Prism(int rOffsetX, int rOffsetY, int gOffsetX, int gOffsetY, int bOffsetX, int bOffsetY, Bitmap orig)
        {
            if (orig != null)
            {
                Bitmap toDraw = new Bitmap(orig, orig.Size);
                BitmapChest origChest = new BitmapChest((Bitmap)orig);
                BitmapChest drawChest = new BitmapChest(toDraw);

                //lock the bits before modifying the values
                origChest.LockBits();
                drawChest.LockBits();

                Rectangle origSize = new Rectangle(0, 0, origChest.Width, origChest.Height);

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
                            if (origSize.Contains(new System.Drawing.Point(x + rOffsetX, y + rOffsetY)) &&
                                origSize.Contains(new System.Drawing.Point(x + gOffsetX, y + gOffsetY)) &&
                                origSize.Contains(new System.Drawing.Point(x + bOffsetX, y + bOffsetY)))
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

        #region BlurEffects
        public void BlurPicker(Bitmap original, int intensity, BlurEffect method)
        {
            switch (method)
            {
                case BlurEffect.Gauss:
                    {
                        BlurGauss(original, intensity);
                        break;
                    }
                case BlurEffect.Median:
                    {
                        BlurMedian(original, intensity);
                        break;
                    }
                default:
                    {
                        //maybe show an error here? maybe not necessary, the combobox already checks whether the blur effect is known.
                        break;
                    }
            }

        }

        private void BlurGauss(Bitmap original, int intensity)
        {
            using (GaussianBlur gauss = new GaussianBlur(original))
            {
                RedrawImageAfter(this, new RedrawEventArgs(gauss.Process(intensity)));
            }
        }

        private void BlurMedian(Bitmap original, int intensity)
        {
            using (Bitmap result = MedianBlur.MedianFilter(original, intensity))
            {
                RedrawImageAfter(this, new RedrawEventArgs(result));
            }
        }

        #endregion
        
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
    

        
