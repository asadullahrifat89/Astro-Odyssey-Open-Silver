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
            YDirection = YDirection.UP;
        }

        public void SetAttributes(double speed, double height = 20, double width = 5)
        {
            Speed = speed;
            Height = height;
            Width = width;
        }
    }
}
