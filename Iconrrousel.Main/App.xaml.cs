using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
            var info = new InfoWindow();

            
            main.Show();
            info.Show();
        }

        public static SharedData Data { get; } = new SharedData();
    }

    public class SharedData : INotifyPropertyChanged
    {
        private string _selectedPath;
        private double _winHeight = UIConfiguration.Window.DefaultHeight;
        private double _winWidth = UIConfiguration.Window.DefaultWidth;
        private double _iconHeight;
        private double _iconWidth;
        private string _path;
        private ImageSource _pathImg;

        public string SelectedPath
        {
            get => _selectedPath;
            set => Set(ref _selectedPath, value);
        }

        public string Path
        {
            get => _path;
            set => Set(ref _path, value);
        }

        public ImageSource PathImg
        {
            get => _pathImg;
            set => Set(ref _pathImg, value);
        }


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

        public double IconHeight
        {
            get => _iconHeight;
            set => Set(ref _iconHeight, value);
        }

        public double IconWidth
        {
            get => _iconWidth;
            set => Set(ref _iconWidth, value);
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









        public event PropertyChangedEventHandler PropertyChanged;
    }
}
