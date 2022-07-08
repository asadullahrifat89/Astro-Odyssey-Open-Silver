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
            Health = Health - HealthSlot;
        }
    }
}
