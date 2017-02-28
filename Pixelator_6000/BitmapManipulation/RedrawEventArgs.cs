using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Pixelator_6000
{
    public class RedrawEventArgs : EventArgs
    {
        public RedrawEventArgs(Bitmap imageToRedraw)
        {
            image = imageToRedraw;
        }
        public Bitmap image { get; set; }
    }
}
