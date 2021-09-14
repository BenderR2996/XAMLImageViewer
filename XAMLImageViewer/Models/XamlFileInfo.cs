using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XAMLImageViewer.Models
{
    public struct XamlFileInfo
    {
        public XamlFileInfo(FileInfo file)
        {
            Name = Path.GetFileNameWithoutExtension(file.Name);
            FullName = file.FullName;
            Content = File.ReadAllText(file.FullName);
            IsVisible = true;
        }
        public string Name;
        public string FullName;
        public string Content;
        public bool IsVisible;
    }
}
