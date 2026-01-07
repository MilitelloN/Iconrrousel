using Iconrrousel.Main.Properties;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;


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
        
        string[] _filesDropped = new string[] { };

        public MainWindow()
        {
            DataContext = App.Data;
            ApplySettings();

            InitializeComponent();
            



            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;

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

            /// TO-DO:
            /// UN SCROLL MAS SUAVE
            /// UNA PANTALLA DE CONFIGURACION: , paleta de colores, resize de la ventana (+iconos),
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int monitorIndex = 1;
            /// HACERLO CONFIGURABLE, MOSTRAR EN SETTINGS UN CONTADOR CON EL NUMERO DE MONITORES DETECTADOS
            var screens = System.Windows.Forms.Screen.AllScreens;

            if (monitorIndex < screens.Length)
            {
                Left = screens[monitorIndex].WorkingArea.Left;
                Top = screens[monitorIndex].WorkingArea.Top;
            }

            double screenWidth = screens[monitorIndex].WorkingArea.Width;
            double screenLeft = screens[monitorIndex].WorkingArea.Left;
            double screenTop = screens[monitorIndex].WorkingArea.Top;

            Left = screenLeft + (screenWidth - ActualWidth) / 2;
            Top = screenTop; 
        }

        private void ApplySettings()
        {
            if (File.Exists(App.Data._CONFIG_FILE))
            {
                var json = File.ReadAllText(App.Data._CONFIG_FILE);
                Dictionary<string, object> settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                App.Data.ShowIconNames = settings.ContainsKey("DisplayNames") ? (bool)settings["DisplayNames"] : false;
                App.Data.DarkTheme = settings.ContainsKey("DarkTheme") ? (bool)settings["DarkTheme"] : false;
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
                Tag = "NewIcon",
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
                Foreground = Brushes.White,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = UIConfiguration.Icon.ButtonWidth + 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
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
                Content = panel
            };

            bttn.Click += (s, e) =>
            {
                MessageBox.Show("Icon clicked!");
            };

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Delete Icon" };

            deleteItem.Click += (s, e) =>
            {
                _paths.Remove(item);
                IconsPanel.Children.Remove(bttn);
                updateJson();
            };

            menu.Items.Add(deleteItem);
            bttn.ContextMenu = menu;

            return bttn;
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
            _filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop);

            updatePaths(_filesDropped);
            updateJson();
            updatePanel();
        }

        private void updatePanel()
        {
            IconsPanel.Children.Clear();
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
            foreach (var path in _filesDropped)
            {
                if (!_paths.Contains(path))
                    _paths.Add(path);
            }

            _paths.Sort();
        }

        public static ImageSource GetHighQualityIcon(string path)
        {
            var shellFile = ShellFile.FromFilePath(path);
            Bitmap bitmap = shellFile.Thumbnail.ExtraLargeBitmap;

            return Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
        }


        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset - UIConfiguration.ScrollButtons.ScrollOffset);
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset + UIConfiguration.ScrollButtons.ScrollOffset);
        }

        private void OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            var settWin = new SettingsWindow(this);
            settWin.Owner = this;
            settWin.Show();
        }

        public void ClearAllIcons()
        {
            _paths.Clear();
            updateJson();
            updatePanel();

        }

        public void SetStartup(bool enable)
        {
            const string appName = "Iconroussel";
            string exePath = Assembly.GetExecutingAssembly().Location;

            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                    key.SetValue(appName, exePath);
                else
                    key.DeleteValue(appName, false);
            }
        }
    }





}
