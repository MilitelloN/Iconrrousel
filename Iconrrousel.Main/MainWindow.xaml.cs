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


        string[] _filesDropped = new string[] { };
        List<string> _paths = new List<string>();

        public MainWindow()
        {
            DataContext = App.Data;
            InitializeComponent();

            //MouseDown += (s, e) =>
            //{
            //    if (e.LeftButton == MouseButtonState.Pressed)
            //        DragMove();
            //};

            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;


            // TO BE REMOVED -- START
            SizeChanged += (s, e) =>
            {
                App.Data.WinWidth = ActualWidth;
                App.Data.WinHeight = ActualHeight;
            };
            // TO BE REMOVED -- END
            this.AllowDrop = true;


            if (File.Exists("data.json"))
            {
                var json = File.ReadAllText("data.json");
                var items = JsonConvert.DeserializeObject<List<string>>(json);

                foreach (var item in items)
                {
                    _paths.Add(item);
                    IconsPanel.Children.Add(getButton(item));

                }
            }

   

            /// TO-DO:
            /// UN SCROLL MAS SUAVE
            /// MAS ESTETICA LA VENTANA
            /// UNA PANTALLA DE CONFIGURACION: autostart, paleta de colores, resize de la ventana (+iconos), siempre arriba, mostrar nombres
            /// ELIMINAR ICONOS DE LA LISTA
            /// CUSTOM 'RIGHT CLICK' MENU: abrir, eliminar
            
            
            /// Abrir los iconos
            /// DESAPARECER LA VENTANA
        }

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

        private UIElement getButton(string item)
        {
            Button bttn = new Button
            {
                Width = 70,
                Height = 70,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 10, 10, 0),
                Content = new System.Windows.Controls.Image
                {
                    Tag = "NewIcon",
                    Width = 64,
                    Height = 64,
                    //Margin = new Thickness(10, 10, 10, 0),
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

            return bttn;
        }
       

        private Image getImage(string path)
        {
            System.Windows.Controls.Image img = new System.Windows.Controls.Image
            {
                Tag = "NewIcon",
                Width = 64,
                Height = 64,
                Margin = new Thickness(10, 10, 10, 0),
                Source = IconExtractor.GetJumboIcon(path),
                Stretch = System.Windows.Media.Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            return img;
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
            // Suelto un archivo: Lo agrego a una lista, reordeno, guardo en JSON, mientras reescribo el json puedo hacer un hilo para mostrar los nuevos.

            _filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop);


            var items = new List<StoredItem>();

            foreach (var path in _filesDropped)
            {
                if (!_paths.Contains(path))
                    _paths.Add(path);
            }

            _paths.Sort();

            string json = JsonConvert.SerializeObject(_paths, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("data.json", json);

            IconsPanel.Children.Clear();
            foreach (var path in _paths)
            {
                IconsPanel.Children.Add(getButton(path));
            }



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
            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset - 200);
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset + 200);
        }

        private void OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("New window!");
        }
    }
    class StoredItem
    {
        public string Path { get; set; }
    }

    



}
