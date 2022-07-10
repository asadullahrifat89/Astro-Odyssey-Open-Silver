using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AstroOdyssey
{
    public class Player : GameObject
    {
        public Player()
        {
            Name = "Player";
            Background = new SolidColorBrush(Colors.Transparent);
            Height = 150;
            Width = 100;

            Uri shipUri = null, exhaustUri = null;
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
            
            exhaustUri = new Uri("ms-appx:///Assets/Images/effect_purple.png", UriKind.RelativeOrAbsolute);

            var imgShip = new Image()
            {
                Source = new BitmapImage(shipUri),
                Stretch = Stretch.Uniform,
            };            

            var imgExhaust = new Image()
            {
                Source = new BitmapImage(exhaustUri),
                Stretch = Stretch.Uniform,
                Height = exhaustHeight,
                Width = imgShip.Width
            };

            imgExhaust.Margin = new Windows.UI.Xaml.Thickness(0, 80, 0, 0);

            // create ship and exhaust
            var playerShip = new Grid();
            playerShip.Children.Add(imgExhaust);
            playerShip.Children.Add(imgShip);

            Child = playerShip;
            Health = 100;
            HealthSlot = 10;
        }
    }
}
