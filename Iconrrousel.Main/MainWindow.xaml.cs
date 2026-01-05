using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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


        string[] _filesDropped = new string[] { };

        public MainWindow()
        {
            DataContext = App.Data;
            InitializeComponent();

            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;

            this.AllowDrop = true;


            if (File.Exists(App.Data.FILENAME))
            {
                var json = File.ReadAllText(App.Data.FILENAME);
                var items = JsonConvert.DeserializeObject<List<string>>(json);

                foreach (var item in items)
                {
                    App.Data.Paths.Add(item);
                    IconsPanel.Children.Add(getButton(item));

                }
            }

            /// TO-DO:
            /// UN SCROLL MAS SUAVE

            /// UNA PANTALLA DE CONFIGURACION: autostart, paleta de colores, resize de la ventana (+iconos), siempre arriba, mostrar nombres

        }


        private UIElement getButton(string item)
        {
            Button bttn = new Button
            {
                Width = UIConfiguration.Icon.ButtonWidth,
                Height = UIConfiguration.Icon.ButtonHeight,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = UIConfiguration.Icon.ButtonMargin,
                Content = new System.Windows.Controls.Image
                {
                    Tag = "NewIcon",
                    Width = UIConfiguration.Icon.ImageWidth,
                    Height = UIConfiguration.Icon.ImageHeight,
                    Source = IconExtractor.GetJumboIcon(item),
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };


            bttn.Click += (s, e) =>
            {
                MessageBox.Show("Icon clicked!");
            };

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Delete Icon" };

            deleteItem.Click += (s, e) =>
            {
                App.Data.Paths.Remove(item);
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
            foreach (var path in App.Data.Paths)
            {
                IconsPanel.Children.Add(getButton(path));
            }
        }

        public void updateJson()
        {
            string json = JsonConvert.SerializeObject(App.Data.Paths, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(App.Data.FILENAME, json);
        }

        private void updatePaths(string[] filesDropped)
        {
            foreach (var path in _filesDropped)
            {
                if (!App.Data.Paths.Contains(path))
                    App.Data.Paths.Add(path);
            }

            App.Data.Paths.Sort();
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

        internal void ClearAllIcons()
        {
            App.Data.Paths.Clear();
            updateJson();
            updatePanel();

        }
    }





}
