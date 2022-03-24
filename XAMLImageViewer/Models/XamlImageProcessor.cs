using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace XAMLImageViewer.Models
{
    public class XamlImageProcessor
    {
        public static ListBoxItem GetUIElement(XamlFileInfo xf)
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

        string getXamlWithResources(string in_content)
        {
            var sb = new StringBuilder(in_content);
            sb.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", "");
            sb.Replace("xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"", "");
            sb.Replace("xmlns:System=\"clr-namespace:System;assembly=mscorlib\"", "");
            sb.Replace("  ", " ");

            var content = sb.ToString();

            Dictionary<string, string> resources = new Dictionary<string, string>();
            if (content.Contains("<Rectangle.Resources>"))
                resources = GetResources(content);

            var startFillKey = "DrawingBrush.Drawing>";
            content = content.Substring(0, content.IndexOf($"/{startFillKey}") - 1).Substring(content.IndexOf(startFillKey) + startFillKey.Length);
            var geometryDrawings = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(x => x.Contains("<GeometryDrawing"))
                                            .ToLookup(x => GetKey(x, "Brush=\"{DynamicResource ", "}\""), y => y)
                                            ;
            Dictionary<string, string> geometries = new Dictionary<string, string>();
            foreach (var gd in geometryDrawings)
                foreach (var value in gd)
                    geometries.Add(value,
                                   value.Replace($"Brush=\"{{DynamicResource {gd.Key}}}\"", "")
                                        .Replace(" />", $" >\n<GeometryDrawing.Brush>\n{resources.First(x => x.Key == gd.Key).Value}\n</GeometryDrawing.Brush>\n</GeometryDrawing>")
                                        );
            foreach (var g in geometries)
                content = content.Replace(g.Key, g.Value);

            var removeNames = content.Split('>').Where(x => x.Contains("Name=")).Select(x => GetKey(x, "x:Name=\""));
            foreach (var name in removeNames)
                content = content.Replace($"x:Name=\"{name}\"", "");
            return content;
        }

        string GetKey(string in_x, string keyStart, string keyEnd = "\"")
        {
            var ret = in_x.Substring(in_x.IndexOf(keyStart) + keyStart.Length);
            ret = ret.Substring(0, ret.IndexOf(keyEnd));
            return ret;
        }
        Dictionary<string, string> GetResources(string content)
        {
            var startResKey = "Rectangle.Resources>";
            return content
                   .Substring(0, content.IndexOf("/" + startResKey) - 1)
                   .Substring(content.IndexOf(startResKey) + startResKey.Length)
                   .Replace("/>", "/>\n")
                   .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(x => x.Trim())
                   .Where(x => !string.IsNullOrEmpty(x))
                   .ToDictionary(x => GetKey(x, "x:Key=\""), y => y.Replace($"x:Key=\"{GetKey(y, "x:Key=\"")}\"", ""));
        }
        private string RemoveComments(string in_content)
        {
            var comments = in_content.Split('\n')
                                     .Where(x => x.Contains("<!--"))
                                     .Select(x => $"<!--{GetKey(x, "<!--", "-->")}-->");
            foreach (var c in comments)
                in_content = in_content.Replace(c, "");
            return in_content.Trim();
        }
        public string GetXaml(string content, string in_name = "")
        {
            content = RemoveComments(content);
            if (content.Contains("<Rectangle.Resources>"))
                content = getXamlWithResources(content);
            else
            {
                var startKey = "DrawingBrush.Drawing>";
                content = content.Substring(0, content.IndexOf($"/{startKey}") - 1).Substring(content.IndexOf(startKey) + startKey.Length);
            }

            content = $@"<DrawingImage x:Key=""img_{in_name}"" x:Shared=""false"">
                        <DrawingImage.Drawing>
                           {content}
                        </DrawingImage.Drawing>
                    </DrawingImage>";
            return content;
        }

        public string GetXamlElement(XamlFileInfo xf) => GetXaml(xf.Content, xf.Name);

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
