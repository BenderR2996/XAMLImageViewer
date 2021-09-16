using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace XAMLImageViewer.Views.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object XAMLImageProcessor { get; private set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val)
            {
                if (parameter?.ToString().ToUpper() == "INVERT") val = !val;
                return (val) ? Visibility.Visible : Visibility.Collapsed;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}