using Iconrrousel.Main.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public readonly static string _CONFIG_FILE = "Settings.json";
        private SettingsFields _settings;

        private void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            LoggingConfig.EnsureConfigured();
            var baseMsg = $"[{DateTime.Now:O}] SettingsWindow.{caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        public SettingsWindow()
        {
            Log("Initializing");
            DataContext = App.Data;
            InitializeComponent();
            try
            {
                SettingsFields settings = App.Data.LoadSettings();
                ApplySettings(settings);
            }
            catch (Exception ex)
            {
                Log("Error loading settings", ex: ex);
            }
        }

        private void ApplySettings(SettingsFields settings)
        {
            Log("Applying settings to UI");
            try
            {
                StartUpCbox.IsChecked = settings._startup;
                NamesCbox.IsChecked = settings._displayNames;
                var radButton = RadioStack.Children
                    .OfType<System.Windows.Controls.RadioButton>()
                    .FirstOrDefault(r => r.Tag.ToString() == settings._windowSizeOption.ToString())
                    .IsChecked = true;
                ThemeCombo.SelectedIndex = (int)settings._themeOption;
            }
            catch (Exception ex)
            {
                Log("Error applying settings", ex: ex);
            }
        }

        private void DeleteAllIcons_Click(object sender, RoutedEventArgs e)
        {
            Log("Delete all icons clicked");
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
                    App.IconService.DeleteAllIcons();
                }
            }
            catch (Exception ex)
            {
                Log("Error deleting all icons", ex: ex);
            }
        }

        private int getWindowSizeFromString(string size)
        {
            Log($"Converting window size from string: {size}");
            switch (size)
            {
                case "Small":
                    return 0;
                case "Medium":
                default:
                    return 1;
                case "Large":
                    return 2;
            }
        }


        private void SaveChanges()
        {
            Log("Saving changes");
            try
            {
                var selected = RadioStack.Children
                    .OfType<System.Windows.Controls.RadioButton>()
                    .FirstOrDefault(r => r.IsChecked == true);

                if (selected != null)
                {
                    string value = selected.Tag.ToString(); // Small / Medium / Large
                }

                SettingsFields settings = new SettingsFields((bool)StartUpCbox.IsChecked,
                    (bool)NamesCbox.IsChecked,
                    (SettingsFields.ThemeOption)ThemeCombo.SelectedIndex,
                    (SettingsFields.WindowSizeOption)getWindowSizeFromString(selected.Tag.ToString()));

                SaveSettings(settings);
                App.Data.UpdateMainWindow(settings);
            }
            catch (Exception ex)
            {
                Log("Error saving changes", ex: ex);
            }
        }

        private void CloseWin_Click(object sender, RoutedEventArgs e)
        {
            Log("Close window clicked");
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
                else
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Log("Error closing window", ex: ex);
            }
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Log("Close application clicked");
            try
            {
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Log("Error closing application", ex: ex);
            }
        }



        public void SaveSettings(SettingsFields settings)
        {
            Log("Saving settings to disk");
            try
            {

                string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(AppPaths.DataDir,_CONFIG_FILE), json);
                _settings = settings;
                SetStartup(settings._startup);
            }
            catch (Exception ex)
            {
                Log("Error saving settings", ex: ex);
            }
        }

        public void SetStartup(bool enable)
        {
            Log($"Setting startup to {(enable ? "enabled" : "disabled")}");
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
                Log("Error setting startup", ex: ex);
            }
        }

        public SettingsFields GetSettings()
        {
            Log("Getting settings object");
            return _settings;
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log("Theme selection changed");
        }
    }

}
