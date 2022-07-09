using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AstroOdyssey
{
    public class Enemy : GameObject
    {
        public Enemy()
        {
            Tag = "enemy";
            Height = 100;
            Width = 100;

            Uri shipUri = null;
            var enemyShipType = new Random().Next(1, 6);

            switch (enemyShipType)
            {
                case 1:
                    shipUri = new Uri("ms-appx:///Assets/Images/enemy_A.png", UriKind.RelativeOrAbsolute);
                    Health = 3;
                    break;
                case 2:
                    shipUri = new Uri("ms-appx:///Assets/Images/enemy_B.png", UriKind.RelativeOrAbsolute);
                    Health = 2;
                    break;
                case 3:
                    shipUri = new Uri("ms-appx:///Assets/Images/enemy_C.png", UriKind.RelativeOrAbsolute);
                    Health = 1;
                    break;
                case 4:
                    shipUri = new Uri("ms-appx:///Assets/Images/enemy_D.png", UriKind.RelativeOrAbsolute);
                    Health = 3;
                    break;
                case 5:
                    shipUri = new Uri("ms-appx:///Assets/Images/enemy_E.png", UriKind.RelativeOrAbsolute);
                    Health = 3;
                    break;
            }

            var imgShip = new Image()
            {
                Source = new BitmapImage(shipUri),
                Stretch = Stretch.Uniform,
            };

            Child = imgShip;
        }
    }
}
