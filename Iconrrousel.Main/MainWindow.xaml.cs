using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;


namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DragMoveWindow
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(
            IntPtr hWnd,
            int Msg,
            int wParam,
            int lParam
        );

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Si el click viene de un Button, Image, etc → NO mover ventana
            if (e.OriginalSource is Button ||
                e.OriginalSource is Image ||
                e.OriginalSource is TextBlock)
                return;

            ReleaseCapture();
            SendMessage(
                new WindowInteropHelper(this).Handle,
                0xA1,
                0x2,
                0
            );
        }
        #endregion

        List<string> _paths = new List<string>();
        readonly string _PATHS_FILE = "Paths.json";

        public MainWindow()
        {
            DataContext = App.Data;
            App.Data.LoadSettings();

            InitializeComponent();

            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
            App.IconService.OnDeleteAllIcons += DeleteAllIcons;
            Closed += MainWindow_Closed;

            this.AllowDrop = true;

            if (File.Exists(_PATHS_FILE))
            {
                var json = File.ReadAllText(_PATHS_FILE);
                var items = JsonConvert.DeserializeObject<List<string>>(json);

                foreach (var item in items)
                {
                    _paths.Add(item);
                    IconsPanel.Children.Add(getButton(item));
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            App.IconService.OnDeleteAllIcons -= DeleteAllIcons;
            CleanupIconPanel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int monitorIndex = 0;
            var screens = System.Windows.Forms.Screen.AllScreens;

            if (screens.Length > 1 && monitorIndex < screens.Length)
            {
                monitorIndex = 1;
            }

            if (monitorIndex < screens.Length)
            {
                double screenWidth = screens[monitorIndex].WorkingArea.Width;
                double screenLeft = screens[monitorIndex].WorkingArea.Left;
                double screenTop = screens[monitorIndex].WorkingArea.Top;

                Left = screenLeft + (screenWidth - ActualWidth) / 2;
                Top = screenTop;
            }
        }

        private UIElement getButton(string item)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var sizeBinding = new Binding("ShowIconNames")
            {
                Source = App.Data,
                Converter = (IValueConverter)FindResource("ShowNamesToSize")
            };

            var img = new System.Windows.Controls.Image
            {
                Tag = item,
                Stretch = System.Windows.Media.Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            img.SetBinding(FrameworkElement.WidthProperty, sizeBinding);
            img.SetBinding(FrameworkElement.HeightProperty, sizeBinding);
            img.Source = IconExtractor.GetJumboIcon(item);
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            var nameBlock = new TextBlock
            {
                Text = System.IO.Path.GetFileNameWithoutExtension(item),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = UIConfiguration.Icon.ButtonWidth + 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            nameBlock.SetBinding(TextBlock.ForegroundProperty, new Binding("IconTextForeground") { Source = App.Data });

            var visibilityBinding = new Binding("ShowIconNames")
            {
                Source = App.Data,
                Converter = new BooleanToVisibilityConverter()
            };
            nameBlock.SetBinding(TextBlock.VisibilityProperty, visibilityBinding);

            panel.Children.Add(img);
            panel.Children.Add(nameBlock);

            var bttn = new Button
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 4, 4, 6),
                Margin = UIConfiguration.Icon.ButtonMargin,
                MinWidth = UIConfiguration.Icon.ButtonWidth,
                MinHeight = UIConfiguration.Icon.ButtonHeight,
                Content = panel,
                Tag = item
            };

            bttn.Click += IconButton_Click;

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Delete Icon" };
            deleteItem.Click += DeleteIcon_Click;
            deleteItem.Tag = bttn;

            menu.Items.Add(deleteItem);
            bttn.ContextMenu = menu;

            return bttn;
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }

        private void DeleteIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is Button bttn && bttn.Tag is string item)
            {
                _paths.Remove(item);
                IconsPanel.Children.Remove(bttn);
                IconExtractor.RemoveFromCache(item);
                CleanupButton(bttn);
                updateJson();
            }
        }

        private void CleanupButton(Button bttn)
        {
            if (bttn.ContextMenu != null)
            {
                foreach (MenuItem item in bttn.ContextMenu.Items)
                {
                    item.Click -= DeleteIcon_Click;
                    item.Tag = null;
                }
                bttn.ContextMenu.Items.Clear();
                bttn.ContextMenu = null;
            }

            bttn.Click -= IconButton_Click;
            
            if (bttn.Content is StackPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.Image img)
                    {
                        BindingOperations.ClearAllBindings(img);
                        img.Source = null;
                    }
                    else if (child is TextBlock tb)
                    {
                        BindingOperations.ClearAllBindings(tb);
                    }
                }
                panel.Children.Clear();
            }
            
            bttn.Content = null;
            bttn.Tag = null;
        }

        private void CleanupIconPanel()
        {
            foreach (var child in IconsPanel.Children)
            {
                if (child is Button btn)
                {
                    CleanupButton(btn);
                }
            }
            IconsPanel.Children.Clear();
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            var filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop);

            updatePaths(filesDropped);
            updateJson();
            updatePanel();
        }

        private void updatePanel()
        {
            CleanupIconPanel();
            
            foreach (var path in _paths)
            {
                IconsPanel.Children.Add(getButton(path));
            }
        }

        public void updateJson()
        {
            string json = JsonConvert.SerializeObject(_paths, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_PATHS_FILE, json);
        }

        private void updatePaths(string[] filesDropped)
        {
            foreach (var path in filesDropped)
            {
                if (!_paths.Contains(path))
                    _paths.Add(path);
            }

            _paths.Sort();
        }

        public static ImageSource GetHighQualityIcon(string path)
        {
            using (var shellFile = ShellFile.FromFilePath(path))
            using (var bitmap = shellFile.Thumbnail.ExtraLargeBitmap)
            {
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
                finally
                {
                    IconExtractor.DeleteObject(hBitmap);
                }
            }
        }


        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset - UIConfiguration.ScrollButtons.ScrollOffset);
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset + UIConfiguration.ScrollButtons.ScrollOffset);
        }

        private void DeleteAllIcons()
        {
            _paths.Clear();
            IconExtractor.ClearCache();
            updateJson();
            updatePanel();
        }
    }
}
