using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using XAMLImageViewer.Models;

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

        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register("FilterText", typeof(string), typeof(MainWindowViewModel)
                , new PropertyMetadata(new PropertyChangedCallback((d, e) =>
                {
                    if (d is MainWindowViewModel vm)
                    {
                        IEnumerable<ListBoxItem> images = null;
                        ChangeVisibility setVisibility = vm.SetVisibility;

                        if (string.IsNullOrEmpty(vm.FilterText))
                        {
                            images = vm.Images;
                            App.Current.Dispatcher.BeginInvoke(
                                setVisibility,
                                DispatcherPriority.Input,
                                vm.Images, Visibility.Visible
                            );
                        }
                        else
                        {
                            images = vm.Images.Where(x => !((XamlFileInfo)x.Tag).Name.ToUpper().Contains(vm.FilterText.ToUpper()));
                            App.Current.Dispatcher.BeginInvoke(
                                setVisibility,
                                DispatcherPriority.Input,
                                images, Visibility.Collapsed
                            );
                            images = vm.Images.Where(x => ((XamlFileInfo)x.Tag).Name.ToUpper().Contains(vm.FilterText.ToUpper()));
                            App.Current.Dispatcher.BeginInvoke(
                                setVisibility,
                                DispatcherPriority.Normal,
                                images, Visibility.Visible
                            );
                        }
                    }
                })));

        private RelayCommand filterItems;

        public delegate void ChangeVisibility(IEnumerable<ListBoxItem> items, Visibility visibility);

        private void SetVisibility(IEnumerable<ListBoxItem> images, Visibility visibility)
        { images.ToList().ForEach(x => x.SetValue(UIElement.VisibilityProperty, visibility)); }

        public RelayCommand SearchItems => filterItems ?? (
            filterItems = new RelayCommand(
                (p) =>
                {
                    IEnumerable<ListBoxItem> images = null;

                    images = Images.Where(x => !((XamlFileInfo)x.Tag).Name.ToUpper().Contains(FilterText.ToUpper()));
                    ChangeVisibility setVisibility = SetVisibility;

                    App.Current.Dispatcher.BeginInvoke(
                        setVisibility,
                        DispatcherPriority.Normal,
                        images, Visibility.Collapsed
                        )
                    ;

                    images = Images.Where(x => ((XamlFileInfo)x.Tag).Name.ToUpper().Contains(FilterText.ToUpper()));

                    App.Current.Dispatcher.BeginInvoke(
                        setVisibility,
                        DispatcherPriority.Normal,
                        images, Visibility.Visible
                        )
                    ;


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

        public static readonly DependencyProperty IsEnabledTaskProperty = DependencyProperty.Register("IsRunLoading", typeof(bool), typeof(MainWindowViewModel)
            , new PropertyMetadata(new PropertyChangedCallback((d, e) =>
            {
                if (d is MainWindowViewModel vm)
                {
                    if (!vm.IsRunLoading)
                    {
                        vm.LoadingInfo = vm.SelectedFolder;
                    }
                }
            })));


        RelayCommand cancel = null;
        public RelayCommand Cancel => cancel ?? (cancel =
            new RelayCommand((p) =>
            {
                cts.Cancel();
                IsRunLoading = false;
            },
                (p) => IsRunLoading
        ));

        public string LoadingInfo
        {
            get { return (string)GetValue(LoadingInfoProperty); }
            set { SetValue(LoadingInfoProperty, value); }
        }

        public static readonly DependencyProperty LoadingInfoProperty =
            DependencyProperty.Register("LoadingInfo", typeof(string), typeof(MainWindowViewModel));



        public async Task Loading(string folder, IProgress<double> progress, CancellationToken token)
        {
            progress.Report(0);
            var files = Directory.GetFiles(folder, "*.xaml", SearchOption.AllDirectories)?.Select(x => new FileInfo(x))?.ToHashSet();

            double percentComplete = 0;

            if (files.Count > 0)
            {
                double count = files.Count;

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
                        LoadingInfo = string.Format("{0:0.0} %", percentComplete); //{Images.Count}/{count}
                    }
                    );
                    percentComplete = Images.Count * 100 / count;
                    progress.Report(percentComplete);
                    Task.Delay(1).Wait();
                }
                OnPropertyChanged("Images");
            }
            await Task.Delay(100);
            progress?.Report(0);
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
