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

namespace XAMLImageViewer.Views
{
    public class XAMLImageContentToUIElementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<FileInfo> files)
            {
                return files.Select(file => GetUIElement(file));
            }
            return null;
        }

        private object GetUIElement(FileInfo file)
        {
            Viewbox vb = new Viewbox() { Height = 16, Width = 16 };
            var ui = XamlImageProcessor.ReadImage(file);
            vb.Child = ui;

            StackPanel sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
            sp.Children.Add(vb);
            sp.Children.Add(new TextBlock() { Text = file.Name, Margin = new Thickness(5, 0, 0, 0) });

            var item = new ListBoxItem() { Content = sp, Tag = file.FullName };
            return item;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
