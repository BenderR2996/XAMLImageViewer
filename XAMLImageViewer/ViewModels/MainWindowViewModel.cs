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
using XAMLImageViewer.Models;
using System.Collections.ObjectModel;
using System.Threading;

namespace XAMLImageViewer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly XAMLImageViewer.Models.XamlImageProcessor model = new Models.XamlImageProcessor();

        public ObservableCollection<ListBoxItem> Images { get; set; } = new ObservableCollection<ListBoxItem>();

        #region Searching
        public string FilterText
        {
            get { return (string)GetValue(FilterTextProperty); }
            set { SetValue(FilterTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilterText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register("FilterText", typeof(string), typeof(MainWindowViewModel)
                , new PropertyMetadata(new PropertyChangedCallback((d, e) =>
                {
                    if (d is MainWindowViewModel vm)
                    {
                        if (string.IsNullOrEmpty(vm.FilterText))
                        {
                            vm.Images.ToList().ForEach(x =>
                            {
                                x.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                            });
                        }
                    }
                })));



        private RelayCommand filterItems;
        public RelayCommand SearchItems => filterItems ?? (
            filterItems = new RelayCommand(
                (p) =>
                {
                    var images = Images.Where(x => !((XamlFileInfo)x.Tag).Name.ToUpper().Contains(FilterText.ToUpper()));
                    for (int i = 0; i < images.Count(); i++)
                    {
                        images.ElementAt(i).SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
                    }
                    images = Images.Where(x => ((XamlFileInfo)x.Tag).Name.ToUpper().Contains(FilterText.ToUpper()));
                    for (int i = 0; i < images.Count(); i++)
                    {
                        images.ElementAt(i).SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    }
                    OnPropertyChanged("Images");
                }
                )
            );

        #endregion


        #region Loading Images
        private string SelectedFolder;
        public double Progress
        {
            get { return (double)GetValue(LoadingProgressProperty); }
            set { SetValue(LoadingProgressProperty, value); }
        }

        public static readonly DependencyProperty LoadingProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(MainWindowViewModel));

        CancellationTokenSource cts;

        RelayCommand loadingFiles = null;
        public RelayCommand LoadingFiles => loadingFiles ?? (loadingFiles =
            new RelayCommand(async (p) =>
            {
                var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        IsRunLoading = true;
                        cts = new CancellationTokenSource();
                        CancellationToken token = cts.Token;
                        Images.Clear();
                        await Task.Run(async () =>
                        {
                            var progress = new Progress<double>();
                            progress.ProgressChanged += (s, e) =>
                            {
                                App.Current?.Dispatcher?.Invoke(() => { Progress = e; });
                            };
                            SelectedFolder = fbd.SelectedPath;
                            OnPropertyChanged("SelectedFolder");
                            await Loading(SelectedFolder, progress, token);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        //MessageBox.Show("Canceled");
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        Task.Delay(100).Wait();
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            IsRunLoading = false;
                            OnPropertyChanged("IsEnabledTask");
                        });
                    }
                }
            },
                (p) => !IsRunLoading
        ));

        public bool IsRunLoading
        {
            get { return (bool)GetValue(IsEnabledTaskProperty); }
            set { SetValue(IsEnabledTaskProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledTaskProperty = DependencyProperty.Register("IsRunLoading", typeof(bool), typeof(MainWindowViewModel));

        RelayCommand cancel = null;
        public RelayCommand Cancel => cancel ?? (cancel =
            new RelayCommand((p) =>
            {
                cts.Cancel();
                IsRunLoading = false;
            },
                (p) => IsRunLoading
        ));
        public async Task Loading(string folder, IProgress<double> progress, CancellationToken token)
        {
            progress.Report(0);
            var files = Directory.GetFiles(folder, "*.xaml", SearchOption.AllDirectories)?.Select(x => new FileInfo(x))?.ToHashSet();

            double percentComplete = 0;

            if (files.Count > 0)
            {
                double step = 100.0 / files.Count;
                double percent = 0.0;
                var coef = (step * 5) / step;
                double delta = (step < 1) ? step * coef : step;
                for (int i = 0; i < files.Count(); i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        progress.Report(0);
                        break;
                    }
                    App.Current?.Dispatcher?.Invoke(async () =>
                    {
                        Images?.Add(XamlImageProcessor.GetUIElement(new XamlFileInfo(files.ElementAt(i))));
                    }
                    );
                    if (step <= delta)
                    {
                        percent += step;
                        if (percent >= delta)
                        {
                            percentComplete++;
                            percent = 0;
                        }
                    }
                    else { percentComplete += step; }
                    progress.Report(percentComplete);
                    Task.Delay(2).Wait();
                }
                OnPropertyChanged("Images");
            }
            await Task.Delay(100);
            progress?.Report(0);
        }

        private void LoadingXamlFiles(string folder)
        {
            Images?.Clear();
            var progress = new Progress<double>();
            progress.ProgressChanged += (s, a) =>
            {
                App.Current.Dispatcher.Invoke(() => { Progress = a; OnPropertyChanged("ImagesList"); });

                if (a == 100)
                {
                    OnPropertyChanged("ImagesList");
                    Progress = 0;
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

            });
        }
        #endregion


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


        #region Copy Image





        private RelayCommand copyResource;
        public RelayCommand CopyResource => copyResource ?? (copyResource = new RelayCommand(
        (parametr) =>
        {
            HashSet<XamlFileInfo> files = new HashSet<XamlFileInfo>();
            if (parametr is ICollection col)
            {
                files = col.OfType<FrameworkElement>().Select(x => (XamlFileInfo)x.Tag).ToHashSet();
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
        #endregion
    }
}
