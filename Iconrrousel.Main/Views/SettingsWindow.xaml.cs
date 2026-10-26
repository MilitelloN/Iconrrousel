using Iconrrousel.Main.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private SettingsFields _settings;
        private ObservableCollection<TimeRange> _timeRanges;
        private string _pinHash;
        // Se pide el PIN una sola vez por apertura de la ventana.
        private bool _pinUnlocked;
        // Color base del tema Custom elegido en esta ventana (aun sin guardar).
        private string _customColor = ThemePalette.ToHex(ThemePalette.DefaultBaseColor);
        // true mientras el codigo (no el usuario) escribe el hex en CustomColorText.
        private bool _showingThemeColor;

        public SettingsWindow()
        {
            LoggingConfig.Log("Initializing");
            DataContext = App.Data;
            _timeRanges = new ObservableCollection<TimeRange>();
            InitializeComponent();
            TimeRangesListBox.ItemsSource = _timeRanges;
            // El tema se previsualiza en vivo; si se cierra sin guardar, se vuelve al guardado.
            Closed += (_, __) => RestoreSavedTheme();
            try
            {
                SettingsFields settings = App.Data.LoadSettings();
                ApplySettings(settings);
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error loading settings", ex: ex);
            }
        }

        private void ApplySettings(SettingsFields settings)
        {
            LoggingConfig.Log("Applying settings to UI");
            try
            {
                // Primero el PIN: si algo de abajo falla, no queda en null
                // (eso permitiria crear uno nuevo y pisar el guardado).
                _pinHash = settings._pinHash;
                _pinUnlocked = false;
                _settings = settings;

                _customColor = ThemePalette.ToHex(
                    ThemePalette.TryParseHex(settings._customColor, out Color customColor)
                        ? customColor
                        : ThemePalette.DefaultBaseColor);

                StartUpCbox.IsChecked = settings._startup;
                NamesCbox.IsChecked = settings._displayNames;
                var radButton = RadioStack.Children
                    .OfType<System.Windows.Controls.RadioButton>()
                    .FirstOrDefault(r => r.Tag.ToString() == settings._windowSizeOption.ToString())
                    .IsChecked = true;
                ThemeCombo.SelectedIndex = (int)settings._themeOption;
                // Si el indice no cambio, SelectionChanged no se dispara.
                ShowThemeColor(settings._themeOption);

                _timeRanges.Clear();
                if (settings._timeRanges != null)
                {
                    foreach (var range in settings._timeRanges)
                    {
                        _timeRanges.Add(range);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error applying settings", ex: ex);
            }
        }

        private void AddTimeRange_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Add time range clicked");
            try
            {
                if (!EnsurePinUnlocked())
                    return;

                var dialog = new TimeRangeDialog();
                dialog.Owner = this;
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    _timeRanges.Add(dialog.Result);
                    LoggingConfig.Log($"Time range added: {dialog.Result}");
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error adding time range", ex: ex);
            }
        }

        private void RemoveTimeRange_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Remove time range clicked");
            try
            {
                if (TimeRangesListBox.SelectedItem is TimeRange selected)
                {
                    if (!EnsurePinUnlocked())
                        return;

                    _timeRanges.Remove(selected);
                    LoggingConfig.Log($"Time range removed: {selected}");
                }
                else
                {
                    MessageBox.Show("Please select a time range to remove", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error removing time range", ex: ex);
            }
        }

        // Si todavia no hay PIN, lo crea (el primer cambio de rangos lo define);
        // si ya hay, lo pide. Devuelve false si el usuario cancela o se equivoca.
        private bool EnsurePinUnlocked()
        {
            if (_pinUnlocked)
                return true;

            bool createMode = string.IsNullOrEmpty(_pinHash);
            var dialog = new PinDialog(createMode) { Owner = this };
            if (dialog.ShowDialog() != true)
                return false;

            if (createMode)
            {
                _pinHash = PinLock.CreateHash(dialog.Pin);
            }
            else if (!PinLock.Verify(dialog.Pin, _pinHash))
            {
                MessageBox.Show("PIN incorrecto", "Iconroussel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _pinUnlocked = true;
            return true;
        }

        private void DeleteAllIcons_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Delete all icons clicked");
            try
            {
                DialogResult result = MessageBox.Show(
                     "This actions CAN NOT be undone",
                     "Delete Confirmation",
                     MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question
                 );

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.DeleteAllIcons();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error deleting all icons", ex: ex);
            }
        }

        private void SaveChanges()
        {
            LoggingConfig.Log("Saving changes");
            try
            {
                var selected = RadioStack.Children
                    .OfType<System.Windows.Controls.RadioButton>()
                    .FirstOrDefault(r => r.IsChecked == true);

                SettingsFields settings = new SettingsFields(
                    (bool)StartUpCbox.IsChecked,
                    (bool)NamesCbox.IsChecked,
                    (SettingsFields.ThemeOption)ThemeCombo.SelectedIndex,
                    (SettingsFields.WindowSizeOption)Enum.Parse(typeof(SettingsFields.WindowSizeOption), selected.Tag.ToString()),
                    _timeRanges.ToList(),
                    _pinHash,
                    _customColor);

                SaveSettings(settings);
                App.Data.UpdateMainWindow(settings);
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error saving changes", ex: ex);
            }
        }

        private void CloseWin_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Close window clicked");
            try
            {
                DialogResult result = MessageBox.Show(
                     "Would you like to save the changes?",
                     "Save Confirmation",
                     MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question
                 );

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    SaveChanges();
                }
                
                this.Close();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error closing window", ex: ex);
            }
        }

        // Vuelve la ventana (y el tema previsualizado) al ultimo estado guardado,
        // sin cerrarla.
        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Discard changes clicked");
            try
            {
                if (_settings == null)
                    return;

                ApplySettings(_settings);
                RestoreSavedTheme();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error discarding changes", ex: ex);
            }
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Close application clicked");
            try
            {
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error closing application", ex: ex);
            }
        }

        public void SaveSettings(SettingsFields settings)
        {
            LoggingConfig.Log("Saving settings to disk");
            try
            {
                string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(AppPaths.DataDir, AppPaths.SettingsFileName), json);
                _settings = settings;
                SetStartup(settings._startup);
            }
            catch (Exception ex)
            {
                LoggingConfig.LogAndNotify("No se pudieron guardar los cambios de configuracion.", ex);
            }
        }

        public void SetStartup(bool enable)
        {
            LoggingConfig.Log($"Setting startup to {(enable ? "enabled" : "disabled")}");
            try
            {
                const string appName = "Iconroussel";
                string exePath = Environment.ProcessPath;

                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                        key.SetValue(appName, exePath);
                    else
                        key.DeleteValue(appName, false);
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error setting startup", ex: ex);
            }
        }

        public SettingsFields GetSettings()
        {
            LoggingConfig.Log("Getting settings object");
            return _settings;
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoggingConfig.Log("Theme selection changed");
            // Se dispara durante InitializeComponent, antes de que existan los demas controles.
            if (!IsInitialized)
                return;

            if (ThemeCombo.SelectedIndex < 0)
                return;

            ShowThemeColor((SettingsFields.ThemeOption)ThemeCombo.SelectedIndex);
            PreviewTheme();
        }

        // Muestra el color base del tema: el de un predefinido (acceso rapido, punto
        // de partida para personalizarlo) o el ultimo color Custom elegido.
        private void ShowThemeColor(SettingsFields.ThemeOption theme)
        {
            _showingThemeColor = true;
            CustomColorText.Text = ThemePalette.ToHex(UIConfiguration.ThemeColors.GetBaseColor(theme, _customColor));
            _showingThemeColor = false;
        }

        private void PickCustomColor_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Pick custom color clicked");
            try
            {
                ThemePalette.TryParseHex(_customColor, out Color current);
                using (var dialog = new ColorDialog
                {
                    FullOpen = true,
                    Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B)
                })
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Dispara CustomColorText_TextChanged, que aplica el color.
                        CustomColorText.Text = ThemePalette.ToHex(Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B));
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error picking custom color", ex: ex);
            }
        }

        private void CustomColorText_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Mientras el hex este incompleto/invalido se mantiene el ultimo color valido.
            if (!ThemePalette.TryParseHex(CustomColorText.Text, out Color color))
                return;

            CustomColorButton.Background = new SolidColorBrush(color);
            if (_showingThemeColor)
                return;

            // El usuario cambio el color: pasa a ser el tema Custom.
            _customColor = ThemePalette.ToHex(color);
            if (ThemeCombo.SelectedIndex != (int)SettingsFields.ThemeOption.Custom)
                ThemeCombo.SelectedIndex = (int)SettingsFields.ThemeOption.Custom; // dispara SelectionChanged, que previsualiza
            else
                PreviewTheme();
        }

        private void PreviewTheme()
        {
            if (ThemeCombo.SelectedIndex < 0)
                return;

            App.Data.ApplyTheme((SettingsFields.ThemeOption)ThemeCombo.SelectedIndex, _customColor);
        }

        private void RestoreSavedTheme()
        {
            if (_settings != null)
                App.Data.ApplyTheme(_settings._themeOption, _settings._customColor);
        }
    }
}
