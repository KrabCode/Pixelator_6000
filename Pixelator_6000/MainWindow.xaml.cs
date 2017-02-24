using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace Pixelator_6000
{
    //if you modify the enum: update every switch that uses it    
    public enum KnownImageFormat { bmp, png, jpeg, gif};

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        Logic logic;
        
        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            logic = new Logic();
            logic.RedrawImageAfter += Logic_RedrawImageAfter;
        }

        private EventHandler Logic_RedrawImageAfter(object sender, RedrawEventArgs e)
        {
            Converter pepa = new Converter();
            imageAfter.Source =  (ImageSource)pepa.Convert(e.image, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture);
            return null;
        }
        

        //Actions for buttons for File Operations in gridFileOperations:
        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image (*.png;*.bmp;*.gif;*.jpg;*.jpeg)|*.png;*.bmp;*.gif;*.jpg;*.jpeg|All files (*.*)|*.*";
            ofd.Title = "Load image";
            if ((bool)ofd.ShowDialog())
            {
                //Dialog success: load the file at ofd.FileName and load it to ImgBefore
                BitmapSource imgSource = new BitmapImage(new Uri(ofd.FileName));
                imageBefore.Source = imgSource;
            }
        }

        private void btCommit_Click(object sender, RoutedEventArgs e)
        {
            if (imageAfter.Source != null)
            {
                imageBefore.Source = imageAfter.Source;
            }
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            if(imageAfter.Source != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save image";
                sfd.Filter = "Portable Network Graphic|*.png|Lossless bitmap image|*.bmp|Jpeg compression|*.jpg|Graphic Interchange Format|*.gif";
                KnownImageFormat format = KnownImageFormat.png;

                if ((bool)sfd.ShowDialog())
                {
                    string ext = System.IO.Path.GetExtension(sfd.FileName);
                    switch (ext)
                    {
                        case ".jpg":
                            format = KnownImageFormat.jpeg;
                            break;
                        case ".bmp":
                            format = KnownImageFormat.bmp;
                            break;
                        case ".gif":
                            format = KnownImageFormat.gif;
                            break;
                    }
                    SaveImageToFile(sfd.FileName, (BitmapImage)imageAfter.Source, format);
                }
            }
        }


        //Tab Pixelsort

        //Pixelsort settings
        public bool PsBright = true;
        public Orientation PsOrientation = Orientation.up;
        public int PsLimit = 50;
        //TODO: Based not only on Saturation, but also Hue
        private void cbPixelsortBrightness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox lb = (ComboBox)sender;
            if (lb.SelectedItem.ToString() == "Bright")
            {

            }
        }

        private void cbPixelsortOrientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void sliderPixelsortLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void btPixelsortApply_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource mySource = (BitmapSource)imageBefore.Source;            
            logic.PixelsortBySaturation(PsBright, PsOrientation, PsLimit, BitmapSourceToBitmap(mySource));
        }

        public static System.Drawing.Bitmap BitmapSourceToBitmap(BitmapSource srs)
        {
            int width = srs.PixelWidth;
            int height = srs.PixelHeight;
            int stride = width * ((srs.Format.BitsPerPixel + 7) / 8);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(height * stride);
                srs.CopyPixels(new Int32Rect(0, 0, width, height), ptr, height * stride, stride);
                using (var btm = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format1bppIndexed, ptr))
                {
                    // Clone the bitmap so that we can dispose it and
                    // release the unmanaged memory at ptr
                    return new System.Drawing.Bitmap(btm);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public static void SaveImageToFile(string filePath, BitmapImage image, KnownImageFormat format)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = null;
                switch (format)
                {
                    case KnownImageFormat.png:
                        {
                            encoder = new PngBitmapEncoder();
                            break;
                        }
                    case KnownImageFormat.bmp:
                        {
                            encoder = new BmpBitmapEncoder();
                            break;
                        }
                    case KnownImageFormat.gif:
                        {
                            encoder = new GifBitmapEncoder();
                            break;
                        }
                    case KnownImageFormat.jpeg:
                        {
                            encoder = new JpegBitmapEncoder();
                            break;
                        }
                }
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }
    }
}
