﻿using System;
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
        bool fullyLoaded = false;
        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            logic = new Logic();
            logic.RedrawImageAfter += Logic_RedrawImageAfter;
            fullyLoaded = true;
        }

        private EventHandler Logic_RedrawImageAfter(object sender, RedrawEventArgs e)
        {
            imageAfter.Source = BitmapConverter.Bitmap2BitmapSource((Bitmap)e.image.Clone());
            return null;
        }

        #region FileButtons
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

                    SaveImageToFile(sfd.FileName,
                        BitmapConverter.BitmapSource2Bitmap((BitmapSource)imageAfter.Source),
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

        //Tab Pixelsort
        #region Pixelsort
        //Pixelsort settings
        public bool PsBright = true;
        public Orientation PsOrientation = Orientation.right;
        public float PsLimit = 0.5f;
        //TODO: Based not only on Saturation, but also Hue
        private void cbPixelsortBrightness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox lb = (ComboBox)sender;
            ComboBoxItem item = (ComboBoxItem)lb.SelectedItem;
            if (item.Content.ToString() == "Bright")
            {
                PsBright = true;
            }
            else
            {
                PsBright = false;
            }
        }

        private void cbPixelsortOrientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int orientationIndex = cbPixelsortOrientation.SelectedIndex;            
            switch(orientationIndex)
            {
                case 0:
                    PsOrientation = Orientation.up;
                    break;
                case 1:
                    PsOrientation = Orientation.down;
                    break;
                case 2:
                    PsOrientation = Orientation.right;
                    break;
                case 3:
                    PsOrientation = Orientation.left;
                    break;
            }            
        }

        private void sliderPixelsortLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float newValue = (float)e.NewValue / 100;
            if (fullyLoaded)
            {
                lbPixelsortLimit.Text = "Limit: " + Math.Round(newValue,2).ToString();
                PsLimit = newValue;
            }
        }

        private void btPixelsortApply_Click(object sender, RoutedEventArgs e)
        {
            if(imageBefore.Source != null)
            {
                BitmapImage mySource = imageBefore.Source as BitmapImage;                
                logic.PixelsortByBrightness(PsBright,
                    PsOrientation,
                    PsLimit,
                    BitmapConverter.BitmapImage2Bitmap(mySource));
            }
            else
            {
                MessageBox.Show("Load an image first.");
            }
        }
        #endregion Pixelsort




        #region Prism

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
        }

        private void prismSliderRY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //minus because the slider should be the other way around: +Y means down, -Y means up
            rOffsetY = -(int)e.NewValue;
            lbPrismInfotextR.Content = "Red offset X:" + rOffsetX + ", Y:" + rOffsetY;
        }

        private void prismSliderGX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            gOffsetX = (int)e.NewValue;
            lbPrismInfotextG.Content = "Green offset X:" + gOffsetX + ", Y:" + gOffsetY;
        }

        private void prismSliderGY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            gOffsetY = -(int)e.NewValue;
            lbPrismInfotextG.Content = "Green offset X:" + gOffsetX + ", Y:" + gOffsetY;
        }

        private void prismSliderBX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bOffsetX = (int)e.NewValue;
            lbPrismInfotextB.Content = "Blue offset X:" + bOffsetX + ", Y:" + bOffsetY;
        }

        private void prismSliderBY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bOffsetY = -(int)e.NewValue;
            lbPrismInfotextB.Content = "Blue offset X:" + bOffsetX + ", Y:" + bOffsetY;
        }

        private void btPrismApply_Click(object sender, RoutedEventArgs e)
        {
            if (imageBefore.Source != null)
            {
                BitmapImage mySource = imageBefore.Source as BitmapImage;
                logic.Prism(rOffsetX, 
                    rOffsetY, 
                    gOffsetX, 
                    gOffsetY, 
                    bOffsetX, 
                    bOffsetY,
                    BitmapConverter.BitmapImage2Bitmap(mySource));
            }
            else
            {
                MessageBox.Show("Load an image first.");
            }
        }
    }
}
#endregion Prism