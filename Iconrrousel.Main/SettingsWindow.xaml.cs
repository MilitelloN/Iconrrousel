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
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow _main;
        public SettingsWindow(MainWindow main)
        {
            DataContext = App.Data;
            InitializeComponent();
            _main = main;
        }

        private void DeleteAllIcons_Click(object sender, RoutedEventArgs e)
        {
            _main.ClearAllIcons();
            this.Close();
        }
    }
}
