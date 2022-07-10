using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AstroOdyssey
{
    public class Health : GameObject
    {
        public Health()
        {
            Tag = "health";
            Height = 100;
            Width = 100;

            Uri uri = null;

            uri = new Uri("ms-appx:///Assets/Images/icon_plusSmall.png", UriKind.RelativeOrAbsolute);
            Health = 5;

            var imgHealthPickup = new Image()
            {
                Source = new BitmapImage(uri),
                Stretch = Stretch.Uniform,
            };

            Child = imgHealthPickup;
        }
    }
}
