using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace XAMLImageViewer.Models
{
    public class XamlImageProcessor
    {
        public string GetXamlElement(FileInfo fi)
        {
            var text = File.ReadAllText(fi.FullName);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?s)<!--.*?-->", "").Replace("\r\n\r\n", "");
            text = text.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", $"x:Key=\"{fi.Name.Replace(fi.Extension, "")}\"");
            return text;
        }
        public string GetResourceDictionary(List<FileInfo> files, string @namespace = "MyResourceDictionary")
            => GetResourceDictionary(files.Select(x => GetXamlElement(x)).ToList(), @namespace);
        private string GetResourceDictionary(List<string> images, string @namespace)
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
            using (var reader = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var ui = XamlReader.Load(reader) as FrameworkElement;
                ui.Tag = content;
                return ui;
            }
        }
        public static UIElement ReadImage(FileInfo file)
        {
            using (var reader = new StreamReader(file.FullName))
            {
                var ui = XamlReader.Load(reader.BaseStream) as FrameworkElement;
                ui.Tag = file.FullName;
                return ui;
            }
        }
    }
}
