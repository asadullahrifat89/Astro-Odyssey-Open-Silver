using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace AstroOdyssey
{
    public class AccentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is SolidColorBrush)
            {
                Color oldColor = ((SolidColorBrush)value).Color;
                byte newR = oldColor.R > 40 ? (byte)(oldColor.R - 40) : (byte)0;
                byte newG = oldColor.G > 40 ? (byte)(oldColor.G - 40) : (byte)0;
                byte newB = oldColor.B > 40 ? (byte)(oldColor.B - 40) : (byte)0;
                Color newColor = Color.FromArgb(oldColor.A, newR, newG, newB);
                return new SolidColorBrush(newColor);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
