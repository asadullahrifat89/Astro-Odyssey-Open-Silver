using System;
using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public partial class MainPage : Page
    {
        #region Ctor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods  

        #region Functionality

        /// <summary>
        /// Navigate to the target page.
        /// </summary>
        /// <param name="targetUri"></param>
        public void NavigateToPage(string targetUri)
        {
            switch (targetUri)
            {
                default:
                    {
                        Uri uri = new Uri(targetUri, UriKind.Relative);
                        PageContainerFrame.Source = uri;
                    }
                    break;
            };
        }

        #endregion

        #endregion
    }
}
