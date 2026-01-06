using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private double _winHeight = UIConfiguration.Window.DefaultHeight;
        private double _winWidth = UIConfiguration.Window.DefaultWidth;
        public readonly string _CONFIG_FILE = "Settings.json";
        private bool _showIconNames;



        public double WinHeight
        {
            get => _winHeight;
            set => Set(ref _winHeight, value);
        }

        public double WinWidth
        {
            get => _winWidth;
            set => Set(ref _winWidth, value);
        }

        public bool ShowIconNames
        {
            get => _showIconNames;
            set => Set(ref _showIconNames, value);
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
        

    }
}
