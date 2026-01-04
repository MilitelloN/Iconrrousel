using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string[] _filesDropped = new string[] { };

        public MainWindow()
        {
            InitializeComponent();

            this.AllowDrop = true;


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

            var file = new FileInfo(_filesDropped[0]);


            InfoLabel.Content =
                $"Nombre: {file.Name}\n" +
                $"Ruta: {file.FullName}\n" +
                $"Tamaño: {file.Length}";

            IconImage.Source = GetFileIcon(file.FullName);

            var items = new List<StoredItem>
            {
                new StoredItem { Path = file.FullName }
            };

            string json = JsonConvert.SerializeObject(items, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("data.json", json);
            
            
            System.Windows.Controls.Image img = new System.Windows.Controls.Image
            {
                Tag = "NewIcon",
                Margin = new Thickness(5),
                Source = GetFileIcon(file.FullName)
            };

            IconsPanel.Children.Add(img);


        }

        BitmapSource GetFileIcon(string path)
        {
            using (Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(48, 48)
                );
            }
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            //var file = new FileInfo(_filesDropped[0]);
            //buttonLabel.Content = file.FullName;

            //buttonGame.Content = _filesDropped[0];
            //if (string.IsNullOrEmpty(file.FullName)) return;
            //Process.Start(new ProcessStartInfo
            //{
            //    FileName = file.FullName,
            //    UseShellExecute = true
            //});


            IconViewer.ScrollToHorizontalOffset(IconViewer.HorizontalOffset + 200);


        }
    }
    class StoredItem
    {
        public string Path { get; set; }
    }
}
