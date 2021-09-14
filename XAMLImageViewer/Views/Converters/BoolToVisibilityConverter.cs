using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;
using System.IO;
using System.Windows;
using System.Text;
using XAMLImageViewer.Models;

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
