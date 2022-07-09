using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public partial class GameStartPage : Page
    {
        #region Ctor

        public GameStartPage()
        {
            InitializeComponent();
        } 

        #endregion

        #region Button Events

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {            
            Application.Current.Host.Content.IsFullScreen = true;            
            App.NavigateToPage("/GamePage");
        }

        #endregion
    }
}
