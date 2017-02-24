using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Pixelator_6000
{
    public enum Orientation { up, down, right, left};
    class Logic
    {
        //global variables with default settings 
        //(must reflect the default window setup)

        public delegate EventHandler RedrawEvent(object sender, RedrawEventArgs e);
        public event RedrawEvent RedrawImageAfter;
        
        public void PixelsortBySaturation(bool bright, Orientation orientation, int limit, Bitmap original)
        {
            //do work            
            RedrawImageAfter(this, new RedrawEventArgs(original));
        }
    }
}
