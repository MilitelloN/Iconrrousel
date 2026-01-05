using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            DataContext = App.Data;
            InitializeComponent();

            //    imgbttn.Content = new Image
            //    {
            //        Source = App.Data.PathImg,
            //        Width = 64,
            //        Height = 64,
            //        VerticalAlignment = VerticalAlignment.Center,
            //        HorizontalAlignment = HorizontalAlignment.Center
            //    };
            //    imgbttn.Click += (s, e) =>
            //    {
            //        MessageBox.Show("Icon clicked!");
            //    };
            //    imgbttn.Width = 70;
            //    imgbttn.Height = 70;
            //    imgbttn.Background = Brushes.Transparent;
            //    imgbttn.BorderBrush = Brushes.Transparent;
        }
    }
}
