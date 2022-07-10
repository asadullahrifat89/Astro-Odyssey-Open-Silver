using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AstroOdyssey
{
    public class Health : GameObject
    {
        private Image content = new Image() { Stretch = Stretch.Uniform };

        public Health()
        {
            Tag = "health";
            Height = 100;
            Width = 100;
            Child = content;

            SetAttributes();
        }

        public void SetAttributes()
        {
            var uri = new Uri("ms-appx:///Assets/Images/icon_plusSmall.png", UriKind.RelativeOrAbsolute);
            Health = 10;

            content.Source = new BitmapImage(uri);
        }
    }
}
