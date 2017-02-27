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
        //Global variables
        private Logic _logic;
        private Bitmap _imageAfterAsBmp;
        private Bitmap _imageBeforeAsBmp;
        private bool _applyNewSettingsAutomatically = false;
        private bool _busy = false;    //Do not set this manually - use the SetBusy(bool) method!
        private bool appFullyLoaded = false;
        private int imagesSaved = 0;

        //Pixelsort settings
        private bool psBright = true;
        private Orientation psOrientation = Orientation.right;
        private float psLimit = 0.5f;
        
                
        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _logic = new Logic();
            _logic.RedrawImageAfter += Logic_RedrawImageAfter;

            appFullyLoaded = true;
        }

        private EventHandler Logic_RedrawImageAfter(object sender, RedrawEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                imageAfter.Source = BitmapConverter.Bitmap2BitmapSource((Bitmap)e.image.Clone());

                SetBusy(false);
            }));
            _imageAfterAsBmp = e.image;            
            return null;
        }
        
        void SetBusy(bool toThis)
        {
            if(toThis && !_busy)
            {
                Mouse.OverrideCursor = Cursors.Wait;
            }
            else if(!toThis && _busy)
            {
                Mouse.OverrideCursor = null;
            }
            _busy = toThis;
        }

        #region FileButtons
        //Buttons on the far left bottom side of the GUI
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
                _imageBeforeAsBmp = BitmapConverter.BitmapSource2Bitmap(imgSource);
            }
        }

        private void btCommit_Click(object sender, RoutedEventArgs e)
        {
            if (imageAfter.Source != null)
            {
                imageBefore.Source = BitmapConverter.Bitmap2BitmapSource((Bitmap)_imageAfterAsBmp.Clone());
                _imageBeforeAsBmp = _imageAfterAsBmp;
            }
        }

        //Checkboxes in the same grid
        private void checkInstant_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)checkInstant.IsChecked;

            _applyNewSettingsAutomatically = isChecked;

            btPixelsortApply.IsEnabled = !isChecked;
            btPrismApply.IsEnabled = !isChecked;
        }

        private void checkCrop_Click(object sender, RoutedEventArgs e)
        {

        }

        
        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            if(imageAfter.Source != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save image";
                sfd.Filter = "Portable Network Graphic|*.png|Lossless bitmap image|*.bmp|Jpeg compression|*.jpg|Graphic Interchange Format|*.gif";
                sfd.FileName += "image_" + ++imagesSaved;
                KnownImageFormat format = KnownImageFormat.png;
                string plus = "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+" + "+";
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

                    SaveImageToFile(sfd.FileName,
                        _imageAfterAsBmp,
                        format);
                }
            }
        }

        


        public static void SaveImageToFile(string filePath, Bitmap image, KnownImageFormat format)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                ImageFormat finalFormat = ImageFormat.Png;
                switch (format)
                {

                    case KnownImageFormat.png:
                        {
                            finalFormat = ImageFormat.Png;
                            break;
                        }
                    case KnownImageFormat.bmp:
                        {
                            finalFormat = ImageFormat.Bmp;
                            break;
                        }
                    case KnownImageFormat.gif:
                        {
                            finalFormat = ImageFormat.Gif;
                            break;
                        }
                    case KnownImageFormat.jpeg:
                        {
                            finalFormat = ImageFormat.Jpeg;
                            break;
                        }
                }
                image.Save(fileStream, finalFormat);
            }
        }
        #endregion FileButtons

        
        #region PixelsortControls
        
        private void cbPixelsortBrightness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox lb = (ComboBox)sender;
            ComboBoxItem item = (ComboBoxItem)lb.SelectedItem;
            if (item.Content.ToString() == "Bright")
            {
                psBright = true;
            }
            else
            {
                psBright = false;
            }

            if(_applyNewSettingsAutomatically)
            {
                TryPixelsort();
            }
        }

        private void cbPixelsortOrientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int orientationIndex = cbPixelsortOrientation.SelectedIndex;            
            switch(orientationIndex)
            {
                case 0:
                    psOrientation = Orientation.up;
                    break;
                case 1:
                    psOrientation = Orientation.down;
                    break;
                case 2:
                    psOrientation = Orientation.right;
                    break;
                case 3:
                    psOrientation = Orientation.left;
                    break;
            }
            
        }

        private void sliderPixelsortLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float newValue = (float)e.NewValue / 100;
            if (appFullyLoaded)
            {
                lbPixelsortLimit.Text = "Limit: " + Math.Round(newValue,2).ToString();
                psLimit = newValue;
                if (_applyNewSettingsAutomatically)
                {
                    TryPixelsort();
                }
            }
        }

        private void btPixelsortApply_Click(object sender, RoutedEventArgs e)
        {
            TryPixelsort();
        }
        
        private void TryPixelsort()
        {
            if (_imageBeforeAsBmp != null)
            {
                if(!_busy) 
                {
                    //imgAfter redraw switches _busy back to false after this pixelsort call finishes
                    //otherwise when _applyNewSettingsAutomatically is true the cpu and memory go through the roof..
                    //..trying to calculate too many similar things at once (when the user uses the sliderPixelsortLimit for example)
                    SetBusy(true);

                    //async so we don't block the UI thread
                    Task t = Task.Run(delegate {
                        _logic.PixelsortByBrightness(psBright,
                        psOrientation,
                        psLimit,
                        new Bitmap(_imageBeforeAsBmp));
                    });
                }
            }
            else
            {
                MessageBox.Show("Load an image first.");
            }
        }
        
        #endregion PixelsortControls
        

        #region PrismControls

        int rOffsetX = 0;
        int rOffsetY = 0;
        int gOffsetX = 0;
        int gOffsetY = 0;
        int bOffsetX = 0;
        int bOffsetY = 0;

        private void prismSliderRX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //"Red offset X:0, Y:0"
            rOffsetX = (int)e.NewValue;
            lbPrismInfotextR.Content = "Red offset X:" + rOffsetX + ", Y:" + rOffsetY;
            if (_applyNewSettingsAutomatically)
            {
                TryPrism();
            }
        }

        private void prismSliderRY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //minus because the Y slider should be the other way around: +Y means down, -Y means up
            rOffsetY = -(int)e.NewValue;
            lbPrismInfotextR.Content = "Red offset X:" + rOffsetX + ", Y:" + rOffsetY;
            if (_applyNewSettingsAutomatically)
            {
                TryPrism();
            }
        }

        private void prismSliderGX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            gOffsetX = (int)e.NewValue;
            lbPrismInfotextG.Content = "Green offset X:" + gOffsetX + ", Y:" + gOffsetY;
            if (_applyNewSettingsAutomatically)
            {
                TryPrism();
            }
        }

        private void prismSliderGY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            gOffsetY = -(int)e.NewValue;
            lbPrismInfotextG.Content = "Green offset X:" + gOffsetX + ", Y:" + gOffsetY;
            if (_applyNewSettingsAutomatically)
            {
                TryPrism();
            }
        }

        private void prismSliderBX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bOffsetX = (int)e.NewValue;
            lbPrismInfotextB.Content = "Blue offset X:" + bOffsetX + ", Y:" + bOffsetY;
            if (_applyNewSettingsAutomatically)
            {
                TryPrism();
            }
        }

        private void prismSliderBY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bOffsetY = -(int)e.NewValue;
            lbPrismInfotextB.Content = "Blue offset X:" + bOffsetX + ", Y:" + bOffsetY;
            if(_applyNewSettingsAutomatically)
            {
                TryPrism();
            }
        }

        private void btPrismApply_Click(object sender, RoutedEventArgs e)
        {
            TryPrism();
        }

        void TryPrism()
        {
            if (_imageBeforeAsBmp != null)
            {
                if(!_busy)
                {
                    SetBusy(true);
                    Task t = Task.Run(delegate {
                        _logic.Prism(rOffsetX, rOffsetY,
                                    gOffsetX, gOffsetY,
                                    bOffsetX, bOffsetY,
                                    new Bitmap(_imageBeforeAsBmp));
                    });
                }
            }
            else
            {
                MessageBox.Show("Load an image first.");
            }
        }





        #endregion PrismControls

        
    }
}