using Iconrrousel.Main.Configuration;
using Iconrrousel.Main.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Iconrrousel.Main.UIConfiguration;

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var main = new MainWindow();

            main.Show();

        }

        public static SharedData Data { get; } = new SharedData();
    }

    public class SharedData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public readonly static string _CONFIG_FILE = "Settings.json";
        private bool _showIconNames;
        private int _themeSelected;

        public bool ShowIconNames
        {
            get => _showIconNames;
            set => Set(ref _showIconNames, value);
        }

        public int ThemeSelected
        {
            get => _themeSelected;
            set => Set(ref _themeSelected, value);
        }

        protected void Set<T>(ref T field, T value,
        [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public SettingsFields LoadSettings()
        {
            SettingsFields settings;
            if (File.Exists(_CONFIG_FILE))
            {
                var json = File.ReadAllText(_CONFIG_FILE);
                settings = JsonConvert.DeserializeObject<SettingsFields>(json);
            }
            else
            {
                settings = new SettingsFields();
                string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_CONFIG_FILE, json);
            }

            UpdateMainWindow(settings);

            return settings;
        }

        public void UpdateMainWindow(SettingsFields settings)
        {
            ShowIconNames = settings._displayNames;
            ThemeSelected = (int)settings._themeOption;
        }


    }
}
