﻿using System;
using Windows.UI.Xaml;

namespace AstroOdyssey
{
    public sealed partial class App : Application
    {
        private static MainPage mainPage;

        public App()
        {
            InitializeComponent();

            Startup += App_Startup;

            mainPage = new MainPage();
            Window.Current.Content = mainPage;

            mainPage.NavigateToPage("/GameStartPage");
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

        public static void NavigateToPage(string targetUri)
        {
            mainPage.NavigateToPage(targetUri);
        }
    }
}
