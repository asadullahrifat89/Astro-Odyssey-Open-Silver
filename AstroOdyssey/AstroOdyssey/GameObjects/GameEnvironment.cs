using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public class GameEnvironment : Canvas
    {
        private readonly List<GameObject> destroyableGameCanvasObjects = new List<GameObject>();

        public GameEnvironment()
        {

        }

        public void SetSize(double height, double width)
        {
            Height = height;
            Width = width;
        }

        public void ClearDestroyableGameObjects() 
        {
        
        }
    }
}
