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

        public bool IsDestroyable => Health <= 0;

        public void LooseHealth()
        {
            Health -= HealthSlot;
        }

        public void GainHealth()
        {
            Health += HealthSlot;
        }

        public void GainHealth(int health)
        {
            Health += health;
        }

        public Rect GetRect()
        {
            return new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), Width, Height);
        }
    }
}
