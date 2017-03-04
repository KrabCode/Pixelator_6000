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

public enum BlurEffect { Gauss, Median, }                  //If you modify this enum, also modify every switch that uses it

namespace Pixelator_6000
{   
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
        public enum KnownImageFormat { bmp, png, jpeg, gif };       //If you modify this enum, also modify every switch that uses it
        

        /// <summary>
        /// Do not set this manually - use the SetBusy(true) method!
        /// </summary>
        private bool _busy = false;
        private bool appFullyLoaded = false;
        private bool _autosave = false;

        //Pixelsort settings
        private bool psBright = true;
        private Orientation psOrientation = Orientation.right;
        private float psLimit = 0.5f;

        //Prism settings
        enum BaseColor { Red, Green, Blue }
        int rOffsetX = 0;
        int rOffsetY = 0;
        int gOffsetX = 0;
        int gOffsetY = 0;
        int bOffsetX = 0;
        int bOffsetY = 0;

        //Prism animation settings
        enum SliderAnimationDirection { Left, Right }
        private bool _animated = true;
        private bool _loop = false;
        private SliderAnimationDirection _animatedDirection = SliderAnimationDirection.Left;
        
        //Blur settings        
        private BlurEffect blurMethod = BlurEffect.Gauss;
        private int blurMagnitude = 0;

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
        
        1) Be aware of the TryEffect obligation. Before you make calls to logic, consider the following:

            Logic does not like to be disturbed. It also helps to keep its memory and cpu costs low.    
            Before you place a brand new memory and cpu intensive call to the almighty Logic, 
            requesting a new image be forged in the fires of the effect,
            do so only if the Logic is not already busy with another task.
            You can help the Logic in his efforts by appropriating the following spell
            while invoking his attention and sending out a background task to the Logical castle:            

            private void Try<EFFECT>()
            {
                if (_imageBeforeAsBmp != null)                                                  //if there is an image loaded to work with
                {
                    if(!_busy)                                                                  //if and only if the Logic is not busy 
                    {                                   
                        SetBusy(true);                                                          //he is now busy
                        Task t = Task.Run(delegate {                                            //dispatch a new Task to create a new Delegate 
                            _logic.EFFECT(new Bitmap(_imageBeforeAsBmp), EFFECT PARAMETERS);    //who calls your Method of choice 
                        });                                                                     // ! let not the parameters of your call to Logic be a question to the value of some slider in the GUI. 
                    }                                                                           // -> everything must already be known: calling "magnitude" works, "slider.Value" doesn't
                
                else                                                                            //if there is no image to work with
                {
                    MessageBox.Show("Load an image first.");                                    //be nice to the User
                }
            }

            For if the GUI sees the Logic itself, it will go blind and the whole GUI realm will freeze three times over.
        

        2) Know that the caller underlings acting above 
        shall never be graced by the voice of the Logic itself
        and so they need not wait for his answer.
        He makes his results known from the Logic only using the following incantation:

        RedrawImageAfter(this, new RedrawEventArgs(resultingImage));

        Making use of the Logic.RedrawImageAfter event 
        to which this very class subscribes a few lines later
        and helps everyone talk only to who they're supposed to
        and nobody needs to get hurt or frozen.
        
                        
        3) Recognize the overlord _applyNewSettingsAutomatically:

        for I have appointed him to watch over all the puny settings sliders and 
        the overworked comboBoxes and the wee labels in the realm of gridEffectParameters
        and under his rule, every puny underling must try to mobilize their respective 
        parent glitch effect to immediate action, if the User wills it.            

        Remember to add something like this under every little change event in settings:

            private void prismSliderGX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) 
            {
                if (appFullyLoaded)     //avoids some exceptions while starting the app coming from the slider's valuechanged event firing prematurely
                {
                    parameterA = (int)e.NewValue;                                       //save the User input relic
                    lbEffectInfo.Content = "Parameter A: " + parameterA;                //change the label text to reflect the new value
                    
                    if (_applyNewSettingsAutomatically)                                 //if the overlord wills it
                    {
                        TryEffect();                                                    //the User wills it
                    }
                }
            }
            
        You must also use the Try<EFFECT> method while the User interacts with the Apply button 
        if you love the GUI and don't want it to freeze over,
        for it makes the User feel listened to and cared for with the immediate feedback.

        Once you observe these golden rules three, 
        there shall be no unexpected heavy traffic in the kingdom 
        and all will be well forever.
         */
        #endregion README
        
        /// <summary>
        /// Use this event to return images from Logic to be rendered in the imageAfter control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private EventHandler Logic_RedrawImageAfter(object sender, RedrawEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                imageAfter.Source = BitmapConverter.Bitmap2BitmapSource(new Bitmap(e.image));
                SetBusy(false);
            }));

            if (_animated)
            {
                UpdateAnimationState();
            }

            if(_autosave)
            {
                Autosave(new Bitmap(e.image));
            }
           

            _imageAfterAsBmp = new Bitmap(e.image);
            

            return null;
        }

        void UpdateAnimationState()
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                //sketchy way to talk to the ui, doesn't let the user interact with it much while it's on

                if (_animatedDirection == SliderAnimationDirection.Left)
                {
                    sliderPixelsortLimit.Value -= 1;
                    if(sliderPixelsortLimit.Value == 0 && _loop)
                    {
                        _animatedDirection = SliderAnimationDirection.Right;
                    }
                }
                else if (_animatedDirection == SliderAnimationDirection.Right)
                {
                    sliderPixelsortLimit.Value += 1;
                    if (sliderPixelsortLimit.Value == 100 && _loop)
                    {
                        _animatedDirection = SliderAnimationDirection.Left;
                    }
                }

            }));

            if (_animated)
            {
                Task t = Task.Run(delegate {
                    _logic.PixelsortByBrightness(psBright,
                    psOrientation,
                    psLimit,
                    new Bitmap(_imageBeforeAsBmp));
                });
            }
        }

        int _autosavedAlready = 0;
        public void Autosave(Bitmap image)
        {
            if (!Directory.Exists("c:\\Pixelator_autosaves"))
            {
                System.IO.Directory.CreateDirectory("c:\\Pixelator_autosaves");
            }
            
            SaveImageToFile("c:\\Pixelator_autosaves\\" + ++_autosavedAlready + ".bmp", new Bitmap(image), KnownImageFormat.bmp);

        }

        /// <summary>
        /// Sets the cursor to its "Wait" animation as long work begins, sets it back as it ends in Logic_RedrawImageAfter.
        /// </summary>
        /// <param name="toThis"></param>
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
            ofd.Multiselect = false;

            if ((bool)ofd.ShowDialog())
            {

                LoadImageFromFilePath(ofd.FileName);
                PopulateImageMetadataGridWithImageMetadata(_imageBeforeAsBmp);
                /*
                _imageName = "";
                List<string> splitName = new List<string>();
                splitName = ofd.SafeFileName.Split('.').ToList();
                splitName.RemoveAt(splitName.Count - 1);
                splitName.RemoveAt(splitName.Count - 2);
                foreach(string charsBetweenDots in splitName)
                {
                    _imageName += charsBetweenDots + ".";
                }
                _imageName.Remove(_imageName.Length - 1, 1);*/
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

        
        

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            if(imageAfter.Source != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save image";
                sfd.Filter = ".png|*.png|.bmp|*.bmp|.jpg|*.jpg|.gif(stationary)|*.gif";
                //find the best possible name for the new file,
                //so that the user doesn't have to type anything or overwrite when lazy
                string initialDirectory =  sfd.InitialDirectory;
                sfd.FileName += "Image_" + GetVacantSuffix();
                KnownImageFormat format;
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
                        default:                            
                            format = KnownImageFormat.png;
                            break;
                            
                    }

                    SaveImageToFile(sfd.FileName,
                        _imageAfterAsBmp,
                        format);
                }
            }
        }

        private string GetVacantSuffix()
        {
            string name = "";
            
            for(int i = 0; i < 8; i++)
            {
                name += GetLetter();
            }

            return name;
        }
        Random _random = new Random();

        public char GetLetter()
        {
            // This method returns a random lowercase letter.
            // ... Between 'a' and 'z' inclusize.
            int num = _random.Next(0, 26); // Zero to 25
            char let = (char)('a' + num);
            return let;
        }

        private void LoadImageFromFilePath(string path)
        {
            using (FileStream inStream = File.Open(path, FileMode.Open))
            {
                try
                {
                    var desiredBitmap = new Bitmap(inStream);
                    //but when calling Logic, it's a lot less work to pass a Bitmap rather than BitmapSource, so let's save that first
                    _imageBeforeAsBmp = new Bitmap(desiredBitmap);
                    //Dialog success: load the file at ofd.FileName and load it to ImgBefore
                    BitmapSource imgSource = BitmapConverter.Bitmap2BitmapSource(desiredBitmap);
                    //imageBefore.Source display it
                    imageBefore.Source = imgSource;
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        /// <summary>
        /// Saves image to specified path without prompting, overwrites any file on filepath, it just doesn't care. Check these things before you call this.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="image"></param>
        /// <param name="format"></param>
        public void SaveImageToFile(string filePath, Bitmap image, KnownImageFormat format)
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
                    default:
                        {
                            finalFormat = ImageFormat.Png;
                            break;
                        }
                }
                image.Save(fileStream, finalFormat);
            }
        }
        #endregion FileButtons

        
        /// <summary>
        /// Responds to an image being dragged from elsewhere and dropped onto the upper grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridImages_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                //load the first item in the files that were dropped - if that item is an image
                LoadImageFromFilePath(files[0]);
                PopulateImageMetadataGridWithImageMetadata(_imageBeforeAsBmp);
            }
        }

        /// <summary>
        /// Populates the semi transparent grid of labels in the top left corner of the app with detailed image information
        /// </summary>
        private void PopulateImageMetadataGridWithImageMetadata(Bitmap image) { 
                   
            lbImgInfo_0.Content = "Resolution: " + image.Width + "x" + image.Height;
        }

        #region PixelsortControls

        private void cbPixelsortBrightness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox lb = (ComboBox)sender;
            ComboBoxItem item = (ComboBoxItem)lb.SelectedItem;
            psBright = (item.Content.ToString() == "Bright");
            
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
                default:
                    {
                        MessageBox.Show("Unknown orientation!");
                        break;
                    }
            }
        }

        /// <summary>
        /// Animation combo box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem itemSelected = e.AddedItems[0] as ComboBoxItem;
            switch (itemSelected.Content as string)
            {
                case "None":
                    {
                        _animated = false;
                        break;
                    }
                case ">>>>":
                    {
                        _animated = true;
                        _animatedDirection = SliderAnimationDirection.Right;
                        TryPixelsort();
                        break;
                    }
                case "<<<<":
                    {
                        _animated = true;
                        _animatedDirection = SliderAnimationDirection.Left;
                        TryPixelsort();
                        break;
                    }
                case "Loop":
                    {
                        _animated = true;
                        _loop = true;
                        TryPixelsort();
                        break;
                    }
                default:
                    {
                        _animated = false;
                        break;
                    }
            }
        }

        private void sliderPixelsortLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float newValue = (float)e.NewValue / 100;
            if (appFullyLoaded)
            {
                lbPixelsortLimit.Text = "Limit: " + (float)e.NewValue / 100;
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
               

        private void canvasRedOffsetSelector_MouseMove(object sender, MouseEventArgs e)
        {
            UpdatePrismSelector(sender, e, BaseColor.Red);
        }

        private void canvasGreenOffsetSelector_MouseMove(object sender, MouseEventArgs e)
        {
            UpdatePrismSelector(sender, e, BaseColor.Green);
        }

        private void canvasBlueOffsetSelector_MouseMove(object sender, MouseEventArgs e)
        {
            UpdatePrismSelector(sender, e, BaseColor.Blue);
        }
        private void canvasRedOffsetSelector_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdatePrismSelector(sender, e, BaseColor.Red);
        }

        private void canvasGreenOffsetSelector_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdatePrismSelector(sender, e, BaseColor.Green);
        }

        private void canvasBlueOffsetSelector_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdatePrismSelector(sender, e, BaseColor.Blue);
        }

        

        private void UpdatePrismSelector(object selectionCanvas, MouseEventArgs e, BaseColor baseColor)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Canvas senderCanvas = selectionCanvas as Canvas;
                System.Windows.Point clickPos = e.GetPosition(senderCanvas);

                //Display ellipse centered at the clickPos
                Ellipse elle = senderCanvas.Children[1] as Ellipse;
                elle.Visibility = Visibility.Visible;
                Canvas.SetLeft(elle, clickPos.X - elle.Width / 2);
                Canvas.SetTop(elle, clickPos.Y - elle.Height / 2);

                //Calculate the actual offset value that is being set, assuming range of values between -10 and 10 for both axes
                var centerX = senderCanvas.Width / 2;
                var centerY = senderCanvas.Height / 2;
                System.Windows.Point clickPosRelativeToCenter = new System.Windows.Point(
                    (int)clickPos.X - (int)centerX,
                    (int)clickPos.Y - (int)centerY
                    );
                System.Windows.Point clickPosWeighedForMax10 = new System.Windows.Point(
                    clickPosRelativeToCenter.X * 10 / (senderCanvas.Width / 2),
                    clickPosRelativeToCenter.Y * 10 / (senderCanvas.Height / 2)
                    );

                //Set the global variables and label text
                switch (baseColor)
                {
                    case BaseColor.Blue:
                        {
                            bOffsetX = (int)clickPosWeighedForMax10.X;
                            bOffsetY = (int)clickPosWeighedForMax10.Y;
                            lbPrismInfotextB.Content = "Blue offset X:" + bOffsetX + ", Y:" + bOffsetY;
                            break;
                        }
                    case BaseColor.Green:
                        {
                            gOffsetX = (int)clickPosWeighedForMax10.X;
                            gOffsetY = (int)clickPosWeighedForMax10.Y;
                            lbPrismInfotextG.Content = "Green offset X:" + gOffsetX + ", Y:" + gOffsetY;
                            break;
                        }
                    case BaseColor.Red:
                        {
                            rOffsetX = (int)clickPosWeighedForMax10.X;
                            rOffsetY = (int)clickPosWeighedForMax10.Y;
                            lbPrismInfotextR.Content = "Red offset X:" + rOffsetX + ", Y:" + rOffsetY;
                            break;
                        }
                }
                if (_applyNewSettingsAutomatically)
                {
                    TryPrism();
                }
            }
        }

        private void btPrismApply_Click(object sender, RoutedEventArgs e)
        {
            TryPrism();
        }

        private void TryPrism()
        {
            if (_imageBeforeAsBmp != null)
            {
                if (!_busy)
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
            if(appFullyLoaded)
            {
                ComboBox blurMethods = (ComboBox)sender;
                ComboBoxItem selectedMethod = (ComboBoxItem)blurMethods.SelectedItem;
                string selectedMethodName = (string)selectedMethod.Content;
                switch (selectedMethodName)
                {
                    case "Gauss blur":
                        {
                            blurMethod = BlurEffect.Gauss;
                            lbMedianWarning.Visibility = Visibility.Hidden;
                            break;
                        }
                    case "Median blur":
                        {
                            blurMethod = BlurEffect.Median;
                            lbMedianWarning.Visibility = Visibility.Visible;
                            break;
                        }
                    default:
                        {
                            lbMedianWarning.Visibility = Visibility.Hidden;
                            MessageBox.Show("Unknown blur method!");
                            break;
                        }
                }
            }
        }

        private void sliderBlurMagnitude_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (appFullyLoaded)
            {

                lbBlurMagnitude.Text = "Blur magnitude: " + (int)sliderBlurMagnitude.Value;
                blurMagnitude = (int)sliderBlurMagnitude.Value;
                
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
            if(_imageBeforeAsBmp != null)
            {
                if (!_busy)
                {
                    SetBusy(true);

                    switch (blurMethod)
                    {
                        case BlurEffect.Gauss:
                            {
                                Task t = Task.Run(delegate {
                                    _logic.BlurGauss(_imageBeforeAsBmp, blurMagnitude);
                                });
                                
                                break;
                            }
                        case BlurEffect.Median:
                            {
                                Task t = Task.Run(delegate {
                                    _logic.BlurMedian(_imageBeforeAsBmp, blurMagnitude);
                                });
                                break;
                            }
                    }
                                                
                }
            }            
            else
            {
                MessageBox.Show("Load an image first.");
            }
        }













        #endregion

        
    }
}

/*
 * 
 *          if(e.LeftButton == MouseButtonState.Pressed)
            {
                Canvas senderCanvas = sender as Canvas;
                System.Windows.Point clickPos = e.GetPosition(senderCanvas);

                ellipseRedOffsetSelected.Visibility = Visibility.Visible;

                Canvas.SetLeft(ellipseRedOffsetSelected, clickPos.X);
                Canvas.SetTop(ellipseRedOffsetSelected, clickPos.Y);

                var centerX = senderCanvas.Width / 2;
                var centerY = senderCanvas.Height / 2;

                System.Windows.Point clickPosRelativeToCenter = new System.Windows.Point(
                    (int)clickPos.X - (int)centerX,
                    (int)clickPos.Y - (int)centerY
                    );

                System.Windows.Point clickPosWeighedForMax10 = new System.Windows.Point(
                    clickPosRelativeToCenter.X * 10 / (senderCanvas.Width / 2),
                    clickPosRelativeToCenter.Y * 10 / (senderCanvas.Height / 2)
                    );

                rOffsetX = (int)clickPosWeighedForMax10.X;
                rOffsetY = (int)clickPosWeighedForMax10.Y;

                lbPrismInfotextR.Content = "Red offset X:" + rOffsetX + ", Y:" + rOffsetY;

                if (_applyNewSettingsAutomatically)
                {
                    TryPrism();
                }
            }
            
     */
