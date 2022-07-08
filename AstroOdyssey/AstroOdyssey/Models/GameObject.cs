using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public class GameObject : Border 
    {
        public GameObject()
        {

        }

        public bool IsMarkedToDelete { get; set; }
    }
}
