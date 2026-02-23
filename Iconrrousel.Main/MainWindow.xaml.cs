using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
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
using Iconrrousel.Main.Services;


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
        private string _PATHS_FILE = "Paths.json";
        public TimeRangeService TimeRangeService { get; private set; }

        private void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            LoggingConfig.EnsureConfigured();
            var baseMsg = $"[{DateTime.Now:O}] {caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        private static void LogStatic(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            LoggingConfig.EnsureConfigured();
            var baseMsg = $"[{DateTime.Now:O}] {caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        public MainWindow()
        {
            Log("Initializing MainWindow");
            DataContext = App.Data;
            var settings = App.Data.LoadSettings();
            InitializeComponent();

            TimeRangeService = new TimeRangeService();
            if (settings._timeRanges != null && settings._timeRanges.Count > 0)
            {
                TimeRangeService.UpdateTimeRanges(settings._timeRanges);
            }

            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
            App.IconService.OnDeleteAllIcons += DeleteAllIcons;
            Closed += MainWindow_Closed;
            SizeChanged += MainWindow_SizeChanged;

            this.AllowDrop = true;

            if (File.Exists(Path.Combine(AppPaths.DataDir, _PATHS_FILE)))
            {
                try
                {
                    var json = File.ReadAllText(Path.Combine(AppPaths.DataDir, _PATHS_FILE));
                    var items = JsonConvert.DeserializeObject<List<string>>(json);

                    foreach (var item in items)
                    {
                        _paths.Add(item);
                        IconsPanel.Children.Add(getButton(item));
                    }
                }
                catch (Exception ex)
                {
                    Log("Error loading paths file", ex: ex);
                }
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log("SizeChanged");
            try
            {
                if (IsLoaded)
                {
                    CenterWindowOnTopOfScreen();
                }
            }
            catch (Exception ex)
            {
                Log("Error in SizeChanged", ex: ex);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Log("Closed event");
            try
            {
                App.IconService.OnDeleteAllIcons -= DeleteAllIcons;
                TimeRangeService?.Stop();
                CleanupIconPanel();
            }
            catch (Exception ex)
            {
                Log("Error in Closed handler", ex: ex);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log("Loaded event");
            try
            {
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                Log("Error in Loaded handler", ex: ex);
            }
        }

        private void CenterWindowOnTopOfScreen()
        {
            Log("Centering window");
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var screen = screens.Length > 1 ? screens[1] : System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                UpdateLayout();

                double screenWidth = screen.WorkingArea.Width;
                double screenLeft = screen.WorkingArea.Left;
                double screenTop = screen.WorkingArea.Top;

                Left = screenLeft + (screenWidth - ActualWidth) / 2;
                Top = screenTop;
            }
            catch (Exception ex)
            {
                Log("Error centering window", ex: ex);
            }
        }

        private UIElement getButton(string item)
        {
            Log($"Creating button for {item}");
            try
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
                    Style = (Style)FindResource("IconButtonStyle"),
                    Content = panel,
                    Tag = item,
                    Margin = UIConfiguration.Icon.ButtonMargin,
                    MinWidth = UIConfiguration.Icon.ButtonWidth,
                    MinHeight = UIConfiguration.Icon.ButtonHeight
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
            catch (Exception ex)
            {
                Log($"Error creating button for {item}", ex: ex);
                throw;
            }
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Icon button clicked");
            try
            {
                if (sender is Button btn && btn.Tag is string path)
                {
                    var timeRanges = App.Data.TimeRanges;
                    var currentTime = DateTime.Now.TimeOfDay;
                    bool executeAllowed = true;
                    foreach (var timeRange in timeRanges)
                    {
                        executeAllowed = executeAllowed && !timeRange.IsInRange(currentTime);
                    }

                    if (!executeAllowed)
                        MessageBox.Show("HACETE UN CURSO CAPO. VOLVE CUANDO ESTES REALMENTE AL PEDO", "No es tiempo de jugar ahora", MessageBoxButton.OK);
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                Log("Error opening icon", ex: ex);
            }
        }

        private void DeleteIcon_Click(object sender, RoutedEventArgs e)
        {
            Log("Delete icon clicked");
            try
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
            catch (Exception ex)
            {
                Log("Error deleting icon", ex: ex);
            }
        }

        private void CleanupButton(Button bttn)
        {
            Log("Cleaning up button");
            try
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
            catch (Exception ex)
            {
                Log("Error cleaning up button", ex: ex);
            }
        }

        private void CleanupIconPanel()
        {
            Log("Cleaning up icon panel");
            try
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
            catch (Exception ex)
            {
                Log("Error cleaning icon panel", ex: ex);
            }
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            Log("DragEnter");
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;
                else
                    e.Effects = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                Log("Error in DragEnter", ex: ex);
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            Log("Drop");
            try
            {
                var filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop);

                updatePaths(filesDropped);
                updateJson();
                updatePanel();
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                Log("Error in Drop", ex: ex);
            }
        }

        private void updatePanel()
        {
            Log("Updating panel");
            try
            {
                CleanupIconPanel();

                foreach (var path in _paths)
                {
                    IconsPanel.Children.Add(getButton(path));
                }
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                Log("Error updating panel", ex: ex);
            }
        }

        public void updateJson()
        {
            Log("Persisting paths to JSON");
            try
            {
                string json = JsonConvert.SerializeObject(_paths, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(AppPaths.DataDir, _PATHS_FILE), json);
            }
            catch (Exception ex)
            {
                Log("Error writing paths file", ex: ex);
            }
        }

        private void updatePaths(string[] filesDropped)
        {
            Log($"Updating paths with {filesDropped?.Length ?? 0} items");
            try
            {
                foreach (var path in filesDropped)
                {
                    if (!_paths.Contains(path))
                        _paths.Add(path);
                }

                _paths.Sort();
            }
            catch (Exception ex)
            {
                Log("Error updating paths", ex: ex);
            }
        }

        public static ImageSource GetHighQualityIcon(string path)
        {
            LogStatic($"Getting high quality icon for {path}");
            try
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
            catch (Exception ex)
            {
                LogStatic("Error getting high quality icon", ex: ex);
                throw;
            }
        }


        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            Log("Scroll left");
            try
            {
                IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset - UIConfiguration.ScrollButtons.ScrollOffset);
            }
            catch (Exception ex)
            {
                Log("Error scrolling left", ex: ex);
            }
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            Log("Scroll right");
            try
            {
                IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset + UIConfiguration.ScrollButtons.ScrollOffset);
            }
            catch (Exception ex)
            {
                Log("Error scrolling right", ex: ex);
            }
        }

        private void DeleteAllIcons()
        {
            Log("Deleting all icons");
            try
            {
                _paths.Clear();
                IconExtractor.ClearCache();
                updateJson();
                updatePanel();
            }
            catch (Exception ex)
            {
                Log("Error deleting all icons", ex: ex);
            }
        }
    }
}
