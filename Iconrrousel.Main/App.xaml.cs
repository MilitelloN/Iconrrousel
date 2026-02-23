using Iconrrousel.Main.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private System.Drawing.Icon _trayIconImage;
        private SettingsWindow _settingsWindow;
        public static IIconService IconService { get; } = new IconService();

        private void EnsureLogging()
        {
            LoggingConfig.EnsureConfigured();
        }

        private void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            var baseMsg = $"[{DateTime.Now:O}] {caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            EnsureLogging();
            Log("Starting application");
            try
            {
                base.OnStartup(e);

                var main = new MainWindow();
                main.Show();

                string iconPath = Path.Combine(App.Data.baseDir, "Resources\\icono.ico");

                _trayIconImage = new System.Drawing.Icon(Path.Combine(App.Data.baseDir, iconPath));
                _trayIcon = new NotifyIcon
                {
                    Icon = _trayIconImage,
                    Visible = true,
                    Text = "Iconroussel"
                };

                _trayIcon.DoubleClick += (s, _) =>
                {
                    Log("Tray double click - showing main window");
                    Current.MainWindow.Show();
                    Current.MainWindow.WindowState = WindowState.Normal;
                    Current.MainWindow.Activate();
                };

                var menu = new ContextMenuStrip();
                menu.Items.Add("Configuración", null, OpenSettings);
                menu.Items.Add("Salir", null, (_, __) => Shutdown());

                _trayIcon.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                Log("Error in OnStartup", ex: ex);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log("Exiting application");
            try
            {
                base.OnExit(e);

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;

                    if (_trayIcon.ContextMenuStrip != null)
                    {
                        _trayIcon.ContextMenuStrip.Dispose();
                        _trayIcon.ContextMenuStrip = null;
                    }

                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                if (_trayIconImage != null)
                {
                    _trayIconImage.Dispose();
                    _trayIconImage = null;
                }
            }
            catch (Exception ex)
            {
                Log("Error in OnExit", ex: ex);
            }
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            Log("Opening settings window");
            try
            {
                if (_settingsWindow == null || !_settingsWindow.IsLoaded)
                {
                    _settingsWindow = new SettingsWindow();
                    _settingsWindow.Closed += (_, __) => _settingsWindow = null;
                }

                _settingsWindow.Show();
                _settingsWindow.Activate();
            }
            catch (Exception ex)
            {
                Log("Error opening settings", ex: ex);
            }
        }

        public static SharedData Data { get; } = new SharedData();
    }

    public static class LoggingConfig
    {
        private static bool _configured;
        private static readonly object _lock = new object();

        public static void EnsureConfigured()
        {
            if (_configured) return;

            lock (_lock)
            {
                if (_configured) return;
                try
                {
                    Trace.AutoFlush = true;
                    const string source = "Iconrousel";
                    const string logName = "Application";

                    if (!EventLog.SourceExists(source))
                    {
                        EventLog.CreateEventSource(source, logName);
                    }

                    Trace.Listeners.Add(new EventLogTraceListener(source));
                    _configured = true;
                }
                catch
                {
                    // do not throw if event log configuration fails
                }
            }
        }
    }

    public class SharedData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        public readonly static string _CONFIG_FILE = "Settings.json";
        private bool _showIconNames;
        private int _themeSelected;
        private int _windowSizeSelected;

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
        private Brush _comboItemForeground = UIConfiguration.ThemeColors.IconTextForegroundBrush;
        private double _windowMaxWidth = 600;

        private void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            var baseMsg = $"[{DateTime.Now:O}] SharedData.{caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

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

        public int WindowSizeSelected
        {
            get => _windowSizeSelected;
            set => Set(ref _windowSizeSelected, value);
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

        public Brush ComboItemForeground
        {
            get => _comboItemForeground;
            set => Set(ref _comboItemForeground, value);
        }

        public double WindowMaxWidth
        {
            get => _windowMaxWidth;
            set => Set(ref _windowMaxWidth, value);
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
            Log("Loading settings file");
            SettingsFields settings;
            try
            {
                if (File.Exists(Path.Combine(AppPaths.DataDir, _CONFIG_FILE)))
                {
                    var json = File.ReadAllText(Path.Combine(AppPaths.DataDir,_CONFIG_FILE));
                    settings = JsonConvert.DeserializeObject<SettingsFields>(json);
                }
                else
                {
                    settings = new SettingsFields();
                    string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(Path.Combine(AppPaths.DataDir,_CONFIG_FILE), json);
                }

                UpdateMainWindow(settings);
                return settings;
            }
            catch (Exception ex)
            {
                Log("Error loading settings", ex: ex);
                settings = new SettingsFields();
                return settings;
            }
        }

        private List<TimeRange> _timeRanges = new List<TimeRange>();

        public List<TimeRange> TimeRanges
        {
            get => _timeRanges;
            set => Set(ref _timeRanges, value);
        }

        public void UpdateMainWindow(SettingsFields settings)
        {
            Log("Updating main window with settings");
            try
            {
                ShowIconNames = settings._displayNames;
                ThemeSelected = (int)settings._themeOption;
                WindowSizeSelected = (int)settings._windowSizeOption;
                TimeRanges = settings._timeRanges ?? new List<TimeRange>();
                ApplyTheme(settings._themeOption);
                ApplyWindowSize(settings._windowSizeOption);
                
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.TimeRangeService != null)
                    {
                        mainWindow.TimeRangeService.UpdateTimeRanges(TimeRanges);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error updating main window", ex: ex);
            }
        }

        private void ApplyTheme(SettingsFields.ThemeOption theme)
        {
            Log($"Applying theme {theme}");
            try
            {
                SolidColorBrush CreateAndFreezeBrush(Color color)
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }

                switch (theme)
                {
                    case SettingsFields.ThemeOption.Light:
                        ScrollButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                        ScrollButtonHover = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x11, 0x11, 0x11));
                        ScrollButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x33, 0x33, 0x33));
                        ScrollButtonForeground = CreateAndFreezeBrush(Color.FromRgb(0x22, 0x22, 0x22));

                        TopButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0x2A, 0x00, 0x00, 0x00));
                        TopButtonHover = CreateAndFreezeBrush(Color.FromArgb(0x3A, 0x00, 0x00, 0x00));
                        TopButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00));
                        TopButtonForeground = CreateAndFreezeBrush(Color.FromRgb(0x22, 0x22, 0x22));

                        BorderBackground = CreateAndFreezeBrush(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF));
                        ShadowColor = Color.FromArgb(0x33, 0x00, 0x00, 0x00);
                        IconTextForeground = CreateAndFreezeBrush(Color.FromRgb(0x22, 0x22, 0x22));
                        ComboItemForeground = IconTextForeground;
                        break;

                    case SettingsFields.ThemeOption.Beige:
                        ScrollButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x4A, 0x39, 0x23));
                        ScrollButtonHover = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x5A, 0x45, 0x28));
                        ScrollButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x6A, 0x50, 0x2D));
                        ScrollButtonForeground = CreateAndFreezeBrush(Color.FromRgb(0x2E, 0x1F, 0x0B));

                        TopButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0x30, 0x68, 0x55, 0x3B));
                        TopButtonHover = CreateAndFreezeBrush(Color.FromArgb(0x44, 0x78, 0x63, 0x44));
                        TopButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0x60, 0x85, 0x6D, 0x4A));
                        TopButtonForeground = CreateAndFreezeBrush(Color.FromRgb(0x2E, 0x1F, 0x0B));

                        BorderBackground = CreateAndFreezeBrush(Color.FromArgb(0xF0, 0xF4, 0xEF, 0xE5));
                        ShadowColor = Color.FromArgb(0x44, 0x3A, 0x2C, 0x19);
                        IconTextForeground = CreateAndFreezeBrush(Color.FromRgb(0x2E, 0x1F, 0x0B));
                        ComboItemForeground = IconTextForeground;
                        break;

                    case SettingsFields.ThemeOption.Blue:
                        ScrollButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x6B, 0xB6, 0xFF));
                        ScrollButtonHover = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x5A, 0xA7, 0xE8));
                        ScrollButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0x4A, 0x94, 0xD1));
                        ScrollButtonForeground = Brushes.White;

                        TopButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0x2E, 0x4A, 0x90, 0xC8));
                        TopButtonHover = CreateAndFreezeBrush(Color.FromArgb(0x40, 0x3E, 0x7F, 0xB3));
                        TopButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0x60, 0x36, 0x6F, 0x9C));
                        TopButtonForeground = Brushes.White;

                        BorderBackground = CreateAndFreezeBrush(Color.FromArgb(0xE0, 0x10, 0x2A, 0x4F));
                        ShadowColor = Color.FromArgb(0x55, 0x10, 0x2A, 0x4F);
                        IconTextForeground = Brushes.White;
                        ComboItemForeground = IconTextForeground;
                        break;

                    case SettingsFields.ThemeOption.Pink:
                        ScrollButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0xF5, 0xC1, 0xE8));
                        ScrollButtonHover = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0xEC, 0xB0, 0xDC));
                        ScrollButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0xD9, 0x97, 0xC8));
                        ScrollButtonForeground = CreateAndFreezeBrush(Color.FromRgb(0x3A, 0x1C, 0x2C));

                        TopButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0x30, 0xEA, 0xB5, 0xE1));
                        TopButtonHover = CreateAndFreezeBrush(Color.FromArgb(0x44, 0xD6, 0x9E, 0xCB));
                        TopButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0x60, 0xC3, 0x86, 0xB6));
                        TopButtonForeground = CreateAndFreezeBrush(Color.FromRgb(0x3A, 0x1C, 0x2C));

                        BorderBackground = CreateAndFreezeBrush(Color.FromArgb(0xE6, 0xF5, 0xE1, 0xEC));
                        ShadowColor = Color.FromArgb(0x55, 0xA8, 0x65, 0x8B);
                        IconTextForeground = CreateAndFreezeBrush(Color.FromRgb(0x3A, 0x1C, 0x2C));
                        ComboItemForeground = IconTextForeground;
                        break;

                    case SettingsFields.ThemeOption.Red:
                        ScrollButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));
                        ScrollButtonHover = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0xE8, 0x58, 0x58));
                        ScrollButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0xFF, 0xD1, 0x48, 0x48));
                        ScrollButtonForeground = Brushes.White;

                        TopButtonBackground = CreateAndFreezeBrush(Color.FromArgb(0x2E, 0xC7, 0x3D, 0x3D));
                        TopButtonHover = CreateAndFreezeBrush(Color.FromArgb(0x40, 0xB0, 0x34, 0x34));
                        TopButtonPressed = CreateAndFreezeBrush(Color.FromArgb(0x60, 0x98, 0x2D, 0x2D));
                        TopButtonForeground = Brushes.White;

                        BorderBackground = CreateAndFreezeBrush(Color.FromArgb(0xE6, 0x40, 0x12, 0x12));
                        ShadowColor = Color.FromArgb(0x55, 0x40, 0x12, 0x12);
                        IconTextForeground = Brushes.White;
                        ComboItemForeground = IconTextForeground;
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
                        ComboItemForeground = IconTextForeground;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Error applying theme", ex: ex);
            }
        }

        private void ApplyWindowSize(SettingsFields.WindowSizeOption option)
        {
            Log($"Applying window size {option}");
            try
            {
                switch (option)
                {
                    case SettingsFields.WindowSizeOption.Small:
                        WindowMaxWidth = 400;
                        break;
                    case SettingsFields.WindowSizeOption.Large:
                        WindowMaxWidth = 1200;
                        break;
                    case SettingsFields.WindowSizeOption.Medium:
                    default:
                        WindowMaxWidth = 800;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Error applying window size", ex: ex);
            }
        }
    }

    public static class AppPaths
    {
        public static string DataDir { get; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Iconrousel");

        static AppPaths()
        {
            Directory.CreateDirectory(DataDir);
        }
    }
}
