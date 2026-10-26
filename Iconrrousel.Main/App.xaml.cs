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

        protected override void OnStartup(StartupEventArgs e)
        {
            LoggingConfig.Log("Starting application");
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
                    LoggingConfig.Log("Tray double click - showing main window");
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
                LoggingConfig.Log("Error in OnStartup", ex: ex);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LoggingConfig.Log("Exiting application");
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
                LoggingConfig.Log("Error in OnExit", ex: ex);
            }
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            LoggingConfig.Log("Opening settings window");
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
                LoggingConfig.Log("Error opening settings", ex: ex);
            }
        }

        public static SharedData Data { get; } = new SharedData();
    }

    // Logging compartido por toda la app. Escribe a %APPDATA%\Iconrousel\log.txt:
    // antes iba al EventLog de Windows, que necesita admin para crear el source,
    // y sin admin se reintentaba (SourceExists + excepcion) en cada Log().
    // Los mensajes informativos solo se escriben en Debug; los errores siempre.
    public static class LoggingConfig
    {
        private const long MaxLogBytes = 1024 * 1024;
        private static readonly object _lock = new object();
        private static readonly string _logFile = Path.Combine(AppPaths.DataDir, "log.txt");

        public static void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
#if !DEBUG
            if (ex == null) return;
#endif
            var line = $"[{DateTime.Now:O}] {caller}: {message}";
            if (ex != null)
            {
                line += $" | Exception: {ex}";
            }
            Debug.WriteLine(line);

            lock (_lock)
            {
                try
                {
                    // Rotacion simple para que el archivo no crezca sin limite.
                    var info = new FileInfo(_logFile);
                    if (info.Exists && info.Length > MaxLogBytes)
                    {
                        File.Move(_logFile, _logFile + ".old", true);
                    }
                    File.AppendAllText(_logFile, line + Environment.NewLine);
                }
                catch
                {
                    // el logging nunca debe tirar la app
                }
            }
        }

        // Para errores que le importan al usuario (persistencia de configuracion/
        // iconos): loguea igual que Log(), pero ademas lo avisa con un mensaje -
        // antes estos catch quedaban completamente silenciosos.
        public static void LogAndNotify(string userMessage, Exception ex, [CallerMemberName] string caller = null)
        {
            Log(userMessage, caller, ex);
            System.Windows.Forms.MessageBox.Show(
                userMessage,
                "Iconroussel",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
    }

    public class SharedData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        private bool _showIconNames;
        private int _themeSelected;
        private int _windowSizeSelected;

        // Los colores se cargan en el constructor (ApplyTheme con el tema por defecto).
        private Brush _scrollButtonBackground;
        private Brush _scrollButtonHover;
        private Brush _scrollButtonPressed;
        private Brush _scrollButtonForeground;

        private Brush _topButtonBackground;
        private Brush _topButtonHover;
        private Brush _topButtonPressed;
        private Brush _topButtonForeground;

        private Brush _borderBackground;
        private Color _shadowColor;
        private Brush _iconTextForeground;
        private Brush _comboItemForeground;
        private Brush _disabledForeground;
        private double _windowMaxWidth = 600;
        private double _iconViewerWidth = 500;
        private int _visibleIconCount = 5;

        public SharedData()
        {
            ApplyTheme(SettingsFields.ThemeOption.Dark, null);
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

        // Texto/iconos de controles deshabilitados.
        public Brush DisabledForeground
        {
            get => _disabledForeground;
            set => Set(ref _disabledForeground, value);
        }

        public double WindowMaxWidth
        {
            get => _windowMaxWidth;
            set => Set(ref _windowMaxWidth, value);
        }

        // Ancho fijo del "viewport" de iconos (VisibleIconCount * IconGrid.ColumnPitch).
        // Distinto de WindowMaxWidth: WindowMaxWidth es el ancho de toda la pill
        // (incluye botones de scroll y paddings), IconViewerWidth es solo el
        // ancho del ScrollViewer de iconos - determina cuantos iconos entran.
        public double IconViewerWidth
        {
            get => _iconViewerWidth;
            set => Set(ref _iconViewerWidth, value);
        }

        // Cantidad de iconos/columnas visibles sin scrollear para el tamaño de
        // ventana elegido (3/5/8/10 para Small/Medium/Large/ExtraLarge).
        public int VisibleIconCount
        {
            get => _visibleIconCount;
            set => Set(ref _visibleIconCount, value);
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
            LoggingConfig.Log("Loading settings file");
            SettingsFields settings;
            try
            {
                if (File.Exists(Path.Combine(AppPaths.DataDir, AppPaths.SettingsFileName)))
                {
                    var json = File.ReadAllText(Path.Combine(AppPaths.DataDir, AppPaths.SettingsFileName));
                    settings = JsonConvert.DeserializeObject<SettingsFields>(json);
                }
                else
                {
                    settings = new SettingsFields();
                    string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(Path.Combine(AppPaths.DataDir, AppPaths.SettingsFileName), json);
                }

                UpdateMainWindow(settings);
                return settings;
            }
            catch (Exception ex)
            {
                LoggingConfig.LogAndNotify("No se pudo cargar la configuracion guardada, se usaran los valores por defecto.", ex);
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
            LoggingConfig.Log("Updating main window with settings");
            try
            {
                ShowIconNames = settings._displayNames;
                ThemeSelected = (int)settings._themeOption;
                WindowSizeSelected = (int)settings._windowSizeOption;
                TimeRanges = settings._timeRanges ?? new List<TimeRange>();
                ApplyTheme(settings._themeOption, settings._customColor);
                ApplyWindowSize(settings._windowSizeOption);
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error updating main window", ex: ex);
            }
        }

        // Publico para que la ventana de configuracion pueda previsualizar el tema
        // en vivo antes de guardarlo.
        public void ApplyTheme(SettingsFields.ThemeOption theme, string customColor)
        {
            LoggingConfig.Log($"Applying theme {theme}");
            try
            {
                SolidColorBrush CreateAndFreezeBrush(Color color)
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }

                Color baseColor = UIConfiguration.ThemeColors.GetBaseColor(theme, customColor);
                var palette = ThemePalette.FromBaseColor(baseColor);

                ScrollButtonBackground = CreateAndFreezeBrush(palette.ScrollButtonBackground);
                ScrollButtonHover = CreateAndFreezeBrush(palette.ScrollButtonHover);
                ScrollButtonPressed = CreateAndFreezeBrush(palette.ScrollButtonPressed);
                ScrollButtonForeground = CreateAndFreezeBrush(palette.ScrollButtonForeground);

                TopButtonBackground = CreateAndFreezeBrush(palette.TopButtonBackground);
                TopButtonHover = CreateAndFreezeBrush(palette.TopButtonHover);
                TopButtonPressed = CreateAndFreezeBrush(palette.TopButtonPressed);
                TopButtonForeground = CreateAndFreezeBrush(palette.TopButtonForeground);

                BorderBackground = CreateAndFreezeBrush(palette.BorderBackground);
                ShadowColor = palette.ShadowColor;
                IconTextForeground = CreateAndFreezeBrush(palette.TextForeground);
                ComboItemForeground = IconTextForeground;
                DisabledForeground = CreateAndFreezeBrush(palette.DisabledForeground);
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error applying theme", ex: ex);
            }
        }

        private void ApplyWindowSize(SettingsFields.WindowSizeOption option)
        {
            LoggingConfig.Log($"Applying window size {option}");
            try
            {
                var tier = UIConfiguration.WindowSizeTiers.Get(option);
                VisibleIconCount = tier.VisibleIconCount;
                WindowMaxWidth = tier.WindowMaxWidth;

                IconViewerWidth = VisibleIconCount * UIConfiguration.IconGrid.ColumnPitch
                    + UIConfiguration.IconViewer.ExtraWidthForPadding;

                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateIconLayout();
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error applying window size", ex: ex);
            }
        }
    }

    public static class AppPaths
    {
        public const string SettingsFileName = "Settings.json";

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
