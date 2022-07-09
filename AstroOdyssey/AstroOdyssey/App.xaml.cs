using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Windows.UI.Xaml;

namespace AstroOdyssey
{
    public sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Startup += App_Startup;

            var mainPage = new MainPage();
            Window.Current.Content = mainPage;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.Message);
            e.Handled = true;
        }
    }
}
