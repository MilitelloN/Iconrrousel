using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        
        Dictionary<string, object> settings = new Dictionary<string, object>();
        public SettingsWindow(MainWindow main)
        {
            DataContext = App.Data;
            InitializeComponent();
            _main = main;

            if (File.Exists(App.Data._CONFIG_FILE))
            {
                var json = File.ReadAllText(App.Data._CONFIG_FILE);
                settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            }
            LoadSettings();
        }

        private void LoadSettings()
        {
            StartUpCbox.IsChecked = settings.ContainsKey("Startup") ? (bool)settings["Startup"] : false;
            NamesCbox.IsChecked = settings.ContainsKey("DisplayNames") ? (bool)settings["DisplayNames"] : false;
            App.Data.ShowIconNames = NamesCbox.IsChecked ?? false;
        }

        private void DeleteAllIcons_Click(object sender, RoutedEventArgs e)
        {
            _main.ClearAllIcons();
            this.Close();
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {

            _main.SetStartup((bool)StartUpCbox.IsChecked);
            App.Data.ShowIconNames = NamesCbox.IsChecked ?? false;

            if(settings.ContainsKey("Startup"))
                settings["Startup"] = (bool)StartUpCbox.IsChecked;
            else
                settings.Add("Startup", (bool)StartUpCbox.IsChecked);

            if (settings.ContainsKey("DisplayNames"))
                settings["DisplayNames"] = App.Data.ShowIconNames;
            else
                settings.Add("DisplayNames", App.Data.ShowIconNames);

            string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(App.Data._CONFIG_FILE, json);
            this.Close();
        }

        private void CloseWin_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}
