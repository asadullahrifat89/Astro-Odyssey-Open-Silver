using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace AstroOdyssey
{
    public class Laser : GameObject
    {
        public Laser()
        {
            Tag = "laser";
            Height = 20;
            Width = 5;
            Background = new SolidColorBrush(Colors.White);
            CornerRadius = new CornerRadius(50);
        }

        public void SetAttributes(double height = 20, double width = 5)
        {
            Height = height;
            Width = width;
        }
    }
}
