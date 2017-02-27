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
using SuperfastBlur;

namespace Pixelator_6000
{

    




    //If you modify this enum, also modify every switch that uses it
    public enum KnownImageFormat { bmp, png, jpeg, gif };

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
        private bool _applyNewSettingsAutomatically = false;        //The overlord himself, look busy!

        /// <summary>
        /// Do not set this manually - use the SetBusy(true) method!
        /// </summary>
        private bool _busy = false;
        private bool appFullyLoaded = false;
        private int imagesSaved = 0;

        //Pixelsort settings
        private bool psBright = true;
        private Orientation psOrientation = Orientation.right;
        private float psLimit = 0.5f;

        //Prism settings
        int rOffsetX = 0;
        int rOffsetY = 0;
        int gOffsetX = 0;
        int gOffsetY = 0;
        int bOffsetX = 0;
        int bOffsetY = 0;


        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _logic = new Logic();
            _logic.RedrawImageAfter += Logic_RedrawImageAfter;

            appFullyLoaded = true;
        }

        #region README
        /*
    
        Hello, traveler. This class is responsible for the GUI, and it must be protected from the freeze. Therefore:

        _--------------------------------------------------------------------------------------------------------------_
        |      If you add a new tab, or even a new glitch effect, be wary. You must obey these golden rules three.     |
        ---------------------------------------------------------------------------------------------------------------
        
        1) Recognize the overlord _applyNewSettingsAutomatically:

            for I have appointed him to watch over all the puny settings sliders and 
            the overworked comboBoxes and the wee labels in the realm of gridEffectParameters
            and under his rule, every puny underling must try to mobilize their respective 
            parent glitch effect to immediate action, if the User wills it.

            

            Remember to add something like this under every little change event in settings
            :

                private void prismSliderGX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) //event made by the designer
                {
                    if (appFullyLoaded) //avoids some exceptions while starting the app coming from the slider's valuechanged event firing prematurely
                    {
                        parameterA = (int)e.NewValue;                                                  //save the user input relic
                        lbEffectInfo.Content = "Parameter A: " + parameterA;                           //change the label text to reflect the new value
                    
                        if (_applyNewSettingsAutomatically)                                             //if the overlord wills it
                        {
                            TryPrism();                                                                 //the user wills it
                        }
                    }
                }
            
            You must also use the Try<EFFECT> interface while the User interacts with the Apply button 
            if you love the GUI and don't want it to freeze over,
            for it makes the user feel listened to and cared for with the immediate feedback.
            
        2) Be aware of the TryEffect obligation. Before you make calls to logic, consider the following:

            The Logical mastermind does not like to be disturbed. It also helps to keep his memory and cpu costs low.    
            Before you place a brand new memory and cpu intensive call to the mastermind, 
            requesting a new image be forged in the fires of the effect,
            do so only if the Mastermind is not already busy with another task.
            You can help the Mastermind in his efforts by appropriating the following spell
            while invoking his attention and sending out a background task to the Logical castle:            

            private void Try<EFFECT>()
            {
                if (_imageBeforeAsBmp != null)                                                  //if there is an image loaded to work with
                {
                    if(!_busy)                                                                  //if and only if the mastermind is not busy 
                    {                                   
                        SetBusy(true);                                                          //he is now busy
                        Task t = Task.Run(delegate {                                            //dispatch a new Task to create a new Delegate 
                            _logic.EFFECT(new Bitmap(_imageBeforeAsBmp), EFFECT PARAMETERS);    // who calls your Method of choice
                        });
                    }
                else                                                                        //otherwise
                {
                    MessageBox.Show("Load an image first.");                                //be nice to the user
                }
            }

            For if the GUI sees the mastermind itself, it will go blind and the whole GUI realm will freeze three times over.
            
        3) Know that the calling underlings acting above 
        shall never be graced by the voice of the Mastermind himself 
        and so they need not wait for his answer.
        He makes his results known from the Logic only using the following incantation:

        RedrawImageAfter(this, new RedrawEventArgs(resultingImage));

        Which makes use of the Logic.RedrawImageAfter event 
        to which this very class subscribes a bit further down
        and helps everyone talk only to who they're supposed to
        and nobody needs to get hurt or frozen.
        
        Once you observe these golden rules three, 
        there shall be no unexpected heavy traffic in the kingdom 
        and all will be well forever.

                
        }

         */
        #endregion README

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
                _busy = toThis;
            }
            else if(!toThis && _busy)
            {
                Mouse.OverrideCursor = null;
                _busy = toThis;
            }
            
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

                using (var bmpTemp = new Bitmap(ofd.FileName))
                {
                    var desiredBitmap = new Bitmap(bmpTemp);
                    //Dialog success: load the file at ofd.FileName and load it to ImgBefore
                    BitmapSource imgSource = BitmapConverter.Bitmap2BitmapSource(desiredBitmap);
                    imageBefore.Source = imgSource;
                    _imageBeforeAsBmp = BitmapConverter.BitmapSource2Bitmap(imgSource);
                }
                
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

        
        //Checkboxes in the same grid:
        private void checkInstant_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)checkInstant.IsChecked;

            _applyNewSettingsAutomatically = isChecked;            
        }

        private void checkCrop_Click(object sender, RoutedEventArgs e)
        {
            //TODO
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

        #region Blur

        private void cbBlurMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void sliderBlurMagnitude_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (appFullyLoaded)
            {
                lbBlurMagnitude.Text = "Blur magnitude: " + (int)sliderBlurMagnitude.Value;
                if (_applyNewSettingsAutomatically)
                {
                    TryBlur();
                }
            }
        }

        private void btBlurApply_Click(object sender, RoutedEventArgs e)
        {
            TryBlur();            
        }

        private void TryBlur()
        {
            if(!_busy)
            {
                SetBusy(true);
                Task t = Task.Run(delegate {
                    _logic.Blur(new Bitmap(_imageBeforeAsBmp), (int)sliderBlurMagnitude.Value);
                });
            }
            else
            {
                MessageBox.Show("Load an image first.");
            }
        }

        #endregion
    }
}