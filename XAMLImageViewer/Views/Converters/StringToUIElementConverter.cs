using System;
using System.Globalization;
using System.Windows.Data;
using XAMLImageViewer.Models;

namespace XAMLImageViewer.Views.Converters
{
    public class StringToUIElementConverter : IValueConverter
    {
        public object XAMLImageProcessor { get; private set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is XamlFileInfo xf)
                return XamlImageProcessor.ReadImage(xf.Content);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
