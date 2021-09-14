using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace XAMLImageViewer.Models
{
    public class XamlImageProcessor
    {
        public static ListBoxItem GetUIElement(XamlFileInfo xf)
        {
            if (xf.IsVisible)
            {
                Viewbox vb = new Viewbox() { Height = 16, Width = 16 };
                var ui = ReadImage(xf.Content);
                vb.Child = ui;

                StackPanel sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
                sp.Children.Add(vb);
                sp.Children.Add(new TextBlock() { Text = xf.Name, Margin = new Thickness(5, 0, 0, 0) });

                var item = new ListBoxItem() { Content = sp, Tag = xf };
                return item;
            }
            return null;
        }


        public string GetXamlElement(XamlFileInfo xf)
        {
            var text = xf.Content;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?s)<!--.*?-->", "").Replace("\r\n\r\n", "");
            text = text.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", $"x:Key=\"{xf.Name}\"");
            return text;
        }



        public string GetResourceDictionary(IEnumerable<XamlFileInfo> files, string @namespace = "MyResourceDictionary")
            => GetResourceDictionary(files.Select(x => GetXamlElement(x)), @namespace);
        private string GetResourceDictionary(IEnumerable<string> images, string @namespace)
        {
            return
            $@"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
            xmlns:local=""clr-namespace:{@namespace}"">
" +
            $"{string.Join("\r\n", images.ToArray())}" +
            @"
</ResourceDictionary>";
        }

        public static UIElement ReadImage(string content)
        {
            var ui = XamlReader.Parse(content) as FrameworkElement;
            return ui;
        }
    }
}
