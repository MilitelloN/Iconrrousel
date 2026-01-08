using Iconrrousel.Main.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public readonly static string _CONFIG_FILE = "Settings.json";
        private SettingsFields _settings;

        public SettingsWindow()
        {
            DataContext = App.Data;
            InitializeComponent();
            SettingsFields settings = App.Data.LoadSettings();
            ApplySettings(settings);
        }

        private void ApplySettings(SettingsFields settings)
        {
            StartUpCbox.IsChecked = settings._startup;
            NamesCbox.IsChecked = settings._displayNames;
            var radButton = RadioStack.Children
                .OfType<System.Windows.Controls.RadioButton>()
                .FirstOrDefault(r => r.Tag.ToString() == settings._windowSizeOption.ToString())
                .IsChecked = true;
            ThemeCombo.SelectedIndex = (int)settings._themeOption;
        }

        private void DeleteAllIcons_Click(object sender, RoutedEventArgs e)
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

        private int getWindowSizeFromString(string size)
        {
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
            //this.Close();
        }

        private void CloseWin_Click(object sender, RoutedEventArgs e)
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

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        

        public void SaveSettings(SettingsFields settings)
        {
            string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_CONFIG_FILE, json);
            _settings = settings;
            SetStartup(settings._startup);
        }

        public void SetStartup(bool enable)
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

        public SettingsFields GetSettings()
        {
            return _settings;
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
