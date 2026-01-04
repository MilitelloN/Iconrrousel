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
using Image = System.Windows.Controls.Image;


namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string[] _filesDropped = new string[] { };
        List<string> _paths = new List<string>();
        Dictionary<string, Image> currentIcons = new Dictionary<string, System.Windows.Controls.Image>();

        public MainWindow()
        {
            DataContext = App.Data;
            InitializeComponent();

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
                    IconsPanel.Children.Add(getImage(item));

                }
            }
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
            //IconsPanel.Children.Add(img);
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
                IconsPanel.Children.Add(getImage(path));
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
    }
    class StoredItem
    {
        public string Path { get; set; }
    }





}
