﻿using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AstroOdyssey
{
    public class Player : GameObject
    {
        private Grid content = new Grid();

        private Image contentShip = new Image() { Stretch = Stretch.Uniform };

        private Image contentTrail = new Image() { Stretch = Stretch.Uniform };

        public Player()
        {
            Background = new SolidColorBrush(Colors.Transparent);
            Height = 150;
            Width = 100;

            Health = 100;
            HealthSlot = 10;

            // create ship and exhaust
            content = new Grid();
            content.Children.Add(contentTrail);
            content.Children.Add(contentShip);

            Child = content;

            SetAttributes();
        }

        public void SetAttributes()
        {            
            Uri shipUri = null;
            var playerShipType = new Random().Next(1, 13);

            double exhaustHeight = 100;

            switch (playerShipType)
            {
                case 1:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_A.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 100;
                    break;
                case 2:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_B.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 50;
                    break;
                case 3:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_C.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 100;
                    break;
                case 4:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_D.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 100;
                    break;
                case 5:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_E.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 100;
                    break;
                case 6:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_F.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 80;
                    break;
                case 7:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_G.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 80;
                    break;
                case 8:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_H.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 80;
                    break;
                case 9:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_I.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 50;
                    break;
                case 10:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_J.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 100;
                    break;
                case 11:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_K.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 50;
                    break;
                case 12:
                    shipUri = new Uri("ms-appx:///Assets/Images/ship_L.png", UriKind.RelativeOrAbsolute);
                    exhaustHeight = 50;
                    break;
            }

            var exhaustUri = new Uri("ms-appx:///Assets/Images/effect_purple.png", UriKind.RelativeOrAbsolute);

            contentShip.Source = new BitmapImage(shipUri);

            contentTrail.Source = new BitmapImage(exhaustUri);
            contentTrail.Height = exhaustHeight;
            contentTrail.Width = contentShip.Width;
            contentTrail.Margin = new Windows.UI.Xaml.Thickness(0, 80, 0, 0);
        }
    }
}