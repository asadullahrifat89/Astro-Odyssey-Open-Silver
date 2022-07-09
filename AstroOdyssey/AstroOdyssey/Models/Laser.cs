using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace AstroOdyssey
{
    public class Laser : GameObject
    {
        public Laser(double height = 20, double width = 5)
        {
            Tag = "laser";
            Height = height;
            Width = width;
            Background = new SolidColorBrush(Colors.White);
            CornerRadius = new CornerRadius(50);
        }
    }
}
