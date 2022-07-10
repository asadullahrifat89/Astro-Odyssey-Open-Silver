﻿using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace AstroOdyssey
{
    public class Star : GameObject
    {
        public Star()
        {
            Tag = "star";

            Uri uri = null;

            var size = 0;

            var starType = new Random().Next(1, 5);

            switch (starType)
            {
                case 1:
                    uri = new Uri("ms-appx:///Assets/Images/star_large.png", UriKind.RelativeOrAbsolute);
                    size = 20;
                    break;
                case 2:
                    uri = new Uri("ms-appx:///Assets/Images/star_medium.png", UriKind.RelativeOrAbsolute);
                    size = 15;
                    break;
                case 3:
                    uri = new Uri("ms-appx:///Assets/Images/star_small.png", UriKind.RelativeOrAbsolute);
                    size = 10;
                    break;
                case 4:
                    uri = new Uri("ms-appx:///Assets/Images/star_tiny.png", UriKind.RelativeOrAbsolute);
                    size = 5;
                    break;
            }

            Height = size;
            Width = size;

            var imgStar = new Image()
            {
                Source = new BitmapImage(uri),
                Stretch = Stretch.Uniform,
            };

            Child = imgStar;
        }
    }
}
