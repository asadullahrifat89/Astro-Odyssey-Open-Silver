using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public class GameObject : Border
    {
        public GameObject()
        {

        }

        public int Health { get; set; }

        public int HealthSlot { get; set; } = 1;

        public bool IsDestroyable { get; set; }

        public bool HasNoHealth => Health <= 0;

        public void GainHealth()
        {
            Health += HealthSlot;
        }

        public void GainHealth(int health)
        {
            Health += health;
        }

        public void LooseHealth()
        {
            Health -= HealthSlot;
        }

        public Rect GetRect()
        {
            return new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), Width, Height);
        }

        public double GetY()
        {
            return Canvas.GetTop(this);
        }

        public double GetX()
        {
            return Canvas.GetLeft(this);
        }

        public void SetY(double top)
        {
            Canvas.SetTop(this, top);
        }

        public void SetX(double left)
        {
            Canvas.SetLeft(this, left);
        }

        public void MoveX(double left, int direction)
        {
            Canvas.SetLeft(this, GetX() + (left * direction));
        }

        public void MoveY(double top, int direction)
        {
            Canvas.SetTop(this, GetY() + (top * direction));
        }

        public void SetPosition(double top, double left)
        {
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
        }

        public void AddToGameEnvironment(double top, double left, GameEnvironment gameEnvironment)
        {
            SetPosition(top, left);

            gameEnvironment.AddGameObject(this);
        }
    }
}
