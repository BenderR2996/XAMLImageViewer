using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

using System.Windows.Forms;


namespace XAMLImageViewer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly XAMLImageViewer.Models.XamlImageProcessor model = new Models.XamlImageProcessor();

        public List<FileInfo> ImagesList { get; set; } = new List<FileInfo>();
        public double LoadingProgress
        {
            get { return (double)GetValue(LoadingProgressProperty); }
            set { SetValue(LoadingProgressProperty, value); }
        }

        public static readonly DependencyProperty LoadingProgressProperty =
            DependencyProperty.Register("LoadingProgress", typeof(double), typeof(MainWindowViewModel));

        #region Theme
        private bool IsWhiteTheme { get; set; } = true;
        public Brush Theme => (IsWhiteTheme) ? Brushes.White : Brushes.DarkGray;
        private RelayCommand changeTheme;
        public RelayCommand ChangeTheme => changeTheme ?? (
             changeTheme = new RelayCommand((p) =>
             {
                 IsWhiteTheme = !IsWhiteTheme;
                 OnPropertyChanged("Theme");
             }));
        #endregion


        #region Xaml Images Folder
        public string SelectedFolder { get; set; }
        private RelayCommand openFolder;
        public RelayCommand OpenFolder => openFolder ?? (
             openFolder = new RelayCommand((p) =>
             {
                 var fbd = new FolderBrowserDialog();
                 if (fbd.ShowDialog() == DialogResult.OK)
                 {
                     SelectedFolder = fbd.SelectedPath;
                     OnPropertyChanged("SelectedFolder");
                     LoadingXamlFiles(SelectedFolder);
                 }
             }));

        private void LoadingXamlFiles(string folder)
        {
            ImagesList?.Clear();
            var progress = new Progress<double>();
            progress.ProgressChanged += (s, a) =>
            {
                LoadingProgress = a;

                if (a == 100)
                {
                    OnPropertyChanged("ImagesList");
                    LoadingProgress = 0;
                }
            };

            if (Directory.Exists(folder))
            {
                LoadFilesAsync(progress, folder);
            }
            OnPropertyChanged("ImagesList");
        }

        private async void LoadFilesAsync(IProgress<double> progress, string folder)
        {
            await Task.Run(() =>
            {
                progress.Report(5);
                var files = Directory.GetFiles(folder, "*.xaml", SearchOption.AllDirectories)?.Select(x => new FileInfo(x))?.ToList();
                progress.Report(20);
                if (files.Count > 0)
                {
                    double step = 80 / files.Count;
                    double counter = 0;
                    foreach (var file in files)
                    {
                        using (var reader = new StreamReader(file.FullName))
                        {
                            Dispatcher.CurrentDispatcher.Invoke(() =>
                            {
                                ImagesList.Add(file);
                            });
                        }
                        counter += step;
                        progress.Report(counter);
                    }
                    progress.Report(100);
                }
            });
        }

        #endregion

        private RelayCommand selectedImageChanged;
        public RelayCommand SelectedImageChanged => selectedImageChanged ?? (selectedImageChanged = new RelayCommand(
        (parametr) =>
        {
            if (parametr != null)
            {

            }
        }
        ));


        private RelayCommand copyResource;
        public RelayCommand CopyResource => copyResource ?? (copyResource = new RelayCommand(
        (parametr) =>
        {
            List<FileInfo> files = new List<FileInfo>();
            if (parametr is ICollection col)
            {
                files = col.OfType<FrameworkElement>().Select(x =>
                {
                    if (x is FrameworkElement elem)
                    {
                        return new FileInfo(x.Tag.ToString());
                    }
                    return null;
                }
                ).OfType<FileInfo>().ToList();
            }

            if (files.Count() > 0)
            {
                string text = "";
                if (files.Count() == 1)
                {
                    var fi = files.First();
                    text = model.GetXamlElement(fi);
                }
                else
                {
                    text = model.GetResourceDictionary(files);
                }
                System.Windows.Clipboard.SetText(text);
            }
        }
        ));
    }
}
