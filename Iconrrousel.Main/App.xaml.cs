using Iconrrousel.Main.Configuration;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIcon _trayIcon;
        private SettingsWindow _settingsWindow;
        public static IIconService IconService { get; } = new IconService();


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var main = new MainWindow();
            
            main.Show();


            _trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("..\\..\\Resources\\icon.ico"),
                Visible = true,
                Text = "Iconroussel"
            };

            _trayIcon.DoubleClick += (s, _) =>
            {
                Current.MainWindow.Show();
                Current.MainWindow.WindowState = WindowState.Normal;
                Current.MainWindow.Activate();
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Configuración", null, OpenSettings);
            menu.Items.Add("Salir", null, (_, __) => Shutdown());

            _trayIcon.ContextMenuStrip = menu;

        }

        private void OpenSettings(object sender, EventArgs e)
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.Closed += (_, __) => _settingsWindow = null;
            }

            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        public static SharedData Data { get; } = new SharedData();
    }

    public class SharedData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public readonly static string _CONFIG_FILE = "Settings.json";
        private bool _showIconNames;
        private int _themeSelected;

        private Brush _scrollButtonBackground = UIConfiguration.ThemeColors.ScrollButtonBackgroundBrush;
        private Brush _scrollButtonHover = UIConfiguration.ThemeColors.ScrollButtonHoverBrush;
        private Brush _scrollButtonPressed = UIConfiguration.ThemeColors.ScrollButtonPressedBrush;
        private Brush _scrollButtonForeground = UIConfiguration.ThemeColors.ScrollButtonForegroundBrush;

        private Brush _topButtonBackground = UIConfiguration.ThemeColors.TopButtonBackgroundBrush;
        private Brush _topButtonHover = UIConfiguration.ThemeColors.TopButtonHoverBrush;
        private Brush _topButtonPressed = UIConfiguration.ThemeColors.TopButtonPressedBrush;
        private Brush _topButtonForeground = UIConfiguration.ThemeColors.TopButtonForegroundBrush;

        private Brush _borderBackground = UIConfiguration.ThemeColors.BorderBackgroundBrush;
        private Color _shadowColor = UIConfiguration.ThemeColors.ShadowColor;
        private Brush _iconTextForeground = UIConfiguration.ThemeColors.IconTextForegroundBrush;

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

        public Brush ScrollButtonBackground
        {
            get => _scrollButtonBackground;
            set => Set(ref _scrollButtonBackground, value);
        }

        public Brush ScrollButtonHover
        {
            get => _scrollButtonHover;
            set => Set(ref _scrollButtonHover, value);
        }

        public Brush ScrollButtonPressed
        {
            get => _scrollButtonPressed;
            set => Set(ref _scrollButtonPressed, value);
        }

        public Brush ScrollButtonForeground
        {
            get => _scrollButtonForeground;
            set => Set(ref _scrollButtonForeground, value);
        }

        public Brush TopButtonBackground
        {
            get => _topButtonBackground;
            set => Set(ref _topButtonBackground, value);
        }

        public Brush TopButtonHover
        {
            get => _topButtonHover;
            set => Set(ref _topButtonHover, value);
        }

        public Brush TopButtonPressed
        {
            get => _topButtonPressed;
            set => Set(ref _topButtonPressed, value);
        }

        public Brush TopButtonForeground
        {
            get => _topButtonForeground;
            set => Set(ref _topButtonForeground, value);
        }

        public Brush BorderBackground
        {
            get => _borderBackground;
            set => Set(ref _borderBackground, value);
        }

        public Color ShadowColor
        {
            get => _shadowColor;
            set => Set(ref _shadowColor, value);
        }

        public Brush IconTextForeground
        {
            get => _iconTextForeground;
            set => Set(ref _iconTextForeground, value);
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
            ApplyTheme(settings._themeOption);
        }

        private void ApplyTheme(SettingsFields.ThemeOption theme)
        {
            switch (theme)
            {
                case SettingsFields.ThemeOption.Light:
                    ScrollButtonBackground = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00));
                    ScrollButtonHover = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00));
                    ScrollButtonPressed = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00));
                    ScrollButtonForeground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));

                    TopButtonBackground = new SolidColorBrush(Color.FromArgb(0x2A, 0x00, 0x00, 0x00));
                    TopButtonHover = new SolidColorBrush(Color.FromArgb(0x3A, 0x00, 0x00, 0x00));
                    TopButtonPressed = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00));
                    TopButtonForeground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));

                    BorderBackground = new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF));
                    ShadowColor = Color.FromArgb(0x33, 0x00, 0x00, 0x00);
                    IconTextForeground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
                    break;

                case SettingsFields.ThemeOption.Beige:
                    ScrollButtonBackground = new SolidColorBrush(Color.FromArgb(0x26, 0x4A, 0x39, 0x23));
                    ScrollButtonHover = new SolidColorBrush(Color.FromArgb(0x33, 0x5A, 0x45, 0x28));
                    ScrollButtonPressed = new SolidColorBrush(Color.FromArgb(0x4D, 0x6A, 0x50, 0x2D));
                    ScrollButtonForeground = new SolidColorBrush(Color.FromRgb(0x2E, 0x1F, 0x0B));

                    TopButtonBackground = new SolidColorBrush(Color.FromArgb(0x30, 0x68, 0x55, 0x3B));
                    TopButtonHover = new SolidColorBrush(Color.FromArgb(0x44, 0x78, 0x63, 0x44));
                    TopButtonPressed = new SolidColorBrush(Color.FromArgb(0x60, 0x85, 0x6D, 0x4A));
                    TopButtonForeground = new SolidColorBrush(Color.FromRgb(0x2E, 0x1F, 0x0B));

                    BorderBackground = new SolidColorBrush(Color.FromArgb(0xF0, 0xF4, 0xEF, 0xE5));
                    ShadowColor = Color.FromArgb(0x44, 0x3A, 0x2C, 0x19);
                    IconTextForeground = new SolidColorBrush(Color.FromRgb(0x2E, 0x1F, 0x0B));
                    break;

                case SettingsFields.ThemeOption.Blue:
                    ScrollButtonBackground = new SolidColorBrush(Color.FromArgb(0x22, 0x6B, 0xB6, 0xFF));
                    ScrollButtonHover = new SolidColorBrush(Color.FromArgb(0x35, 0x5A, 0xA7, 0xE8));
                    ScrollButtonPressed = new SolidColorBrush(Color.FromArgb(0x55, 0x4A, 0x94, 0xD1));
                    ScrollButtonForeground = Brushes.White;

                    TopButtonBackground = new SolidColorBrush(Color.FromArgb(0x2E, 0x4A, 0x90, 0xC8));
                    TopButtonHover = new SolidColorBrush(Color.FromArgb(0x40, 0x3E, 0x7F, 0xB3));
                    TopButtonPressed = new SolidColorBrush(Color.FromArgb(0x60, 0x36, 0x6F, 0x9C));
                    TopButtonForeground = Brushes.White;

                    BorderBackground = new SolidColorBrush(Color.FromArgb(0xE0, 0x10, 0x2A, 0x4F));
                    ShadowColor = Color.FromArgb(0x55, 0x10, 0x2A, 0x4F);
                    IconTextForeground = Brushes.White;
                    break;

                case SettingsFields.ThemeOption.Pink:
                    ScrollButtonBackground = new SolidColorBrush(Color.FromArgb(0x22, 0xF5, 0xC1, 0xE8));
                    ScrollButtonHover = new SolidColorBrush(Color.FromArgb(0x35, 0xEC, 0xB0, 0xDC));
                    ScrollButtonPressed = new SolidColorBrush(Color.FromArgb(0x55, 0xD9, 0x97, 0xC8));
                    ScrollButtonForeground = new SolidColorBrush(Color.FromRgb(0x3A, 0x1C, 0x2C));

                    TopButtonBackground = new SolidColorBrush(Color.FromArgb(0x30, 0xEA, 0xB5, 0xE1));
                    TopButtonHover = new SolidColorBrush(Color.FromArgb(0x44, 0xD6, 0x9E, 0xCB));
                    TopButtonPressed = new SolidColorBrush(Color.FromArgb(0x60, 0xC3, 0x86, 0xB6));
                    TopButtonForeground = new SolidColorBrush(Color.FromRgb(0x3A, 0x1C, 0x2C));

                    BorderBackground = new SolidColorBrush(Color.FromArgb(0xE6, 0xF5, 0xE1, 0xEC));
                    ShadowColor = Color.FromArgb(0x55, 0xA8, 0x65, 0x8B);
                    IconTextForeground = new SolidColorBrush(Color.FromRgb(0x3A, 0x1C, 0x2C));
                    break;

                case SettingsFields.ThemeOption.Red:
                    ScrollButtonBackground = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0x6B, 0x6B));
                    ScrollButtonHover = new SolidColorBrush(Color.FromArgb(0x35, 0xE8, 0x58, 0x58));
                    ScrollButtonPressed = new SolidColorBrush(Color.FromArgb(0x55, 0xD1, 0x48, 0x48));
                    ScrollButtonForeground = Brushes.White;

                    TopButtonBackground = new SolidColorBrush(Color.FromArgb(0x2E, 0xC7, 0x3D, 0x3D));
                    TopButtonHover = new SolidColorBrush(Color.FromArgb(0x40, 0xB0, 0x34, 0x34));
                    TopButtonPressed = new SolidColorBrush(Color.FromArgb(0x60, 0x98, 0x2D, 0x2D));
                    TopButtonForeground = Brushes.White;

                    BorderBackground = new SolidColorBrush(Color.FromArgb(0xE6, 0x40, 0x12, 0x12));
                    ShadowColor = Color.FromArgb(0x55, 0x40, 0x12, 0x12);
                    IconTextForeground = Brushes.White;
                    break;

                case SettingsFields.ThemeOption.Dark:
                default:
                    ScrollButtonBackground = UIConfiguration.ThemeColors.ScrollButtonBackgroundBrush;
                    ScrollButtonHover = UIConfiguration.ThemeColors.ScrollButtonHoverBrush;
                    ScrollButtonPressed = UIConfiguration.ThemeColors.ScrollButtonPressedBrush;
                    ScrollButtonForeground = UIConfiguration.ThemeColors.ScrollButtonForegroundBrush;

                    TopButtonBackground = UIConfiguration.ThemeColors.TopButtonBackgroundBrush;
                    TopButtonHover = UIConfiguration.ThemeColors.TopButtonHoverBrush;
                    TopButtonPressed = UIConfiguration.ThemeColors.TopButtonPressedBrush;
                    TopButtonForeground = UIConfiguration.ThemeColors.TopButtonForegroundBrush;

                    BorderBackground = UIConfiguration.ThemeColors.BorderBackgroundBrush;
                    ShadowColor = UIConfiguration.ThemeColors.ShadowColor;
                    IconTextForeground = UIConfiguration.ThemeColors.IconTextForegroundBrush;
                    break;
            }
        }
    }
}
