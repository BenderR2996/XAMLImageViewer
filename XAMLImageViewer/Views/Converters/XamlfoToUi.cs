using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Markup;
using System.IO;
using System.Windows;
using XAMLImageViewer.Models;


namespace XAMLImageViewer.Views.Converters
{
    public class XamlfoToUi : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HashSet<XamlFileInfo> files)
            {
                 return files.Select(file => XamlImageProcessor.GetUIElement(file));
            }
            return null;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
