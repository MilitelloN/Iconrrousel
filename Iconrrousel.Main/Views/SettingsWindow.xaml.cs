using Iconrrousel.Main.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public SettingsWindow()
        {
            LoggingConfig.Log("Initializing");
            DataContext = App.Data;
            _timeRanges = new ObservableCollection<TimeRange>();
            InitializeComponent();
            TimeRangesListBox.ItemsSource = _timeRanges;
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
                StartUpCbox.IsChecked = settings._startup;
                NamesCbox.IsChecked = settings._displayNames;
                var radButton = RadioStack.Children
                    .OfType<System.Windows.Controls.RadioButton>()
                    .FirstOrDefault(r => r.Tag.ToString() == settings._windowSizeOption.ToString())
                    .IsChecked = true;
                ThemeCombo.SelectedIndex = (int)settings._themeOption;

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
                    _timeRanges.ToList());

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
                string exePath = Assembly.GetExecutingAssembly().Location;

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
        }
    }
}
