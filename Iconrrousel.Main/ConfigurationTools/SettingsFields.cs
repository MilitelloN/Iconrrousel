using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Iconrrousel.Main.Configuration
{
    public class SettingsFields
    {        
        public enum ThemeOption
        {
            Dark,
            Light,
            Beige,
            Blue,
            Pink,
            Red 
        }

        public enum WindowSizeOption
        {
            Small,
            Medium,
            Large
        }

        public ThemeOption _themeOption { get; set; }
        public bool _startup { get; set; }
        public bool _displayNames { get; set; }
        public WindowSizeOption _windowSizeOption { get; set; }
        public List<TimeRange> _timeRanges { get; set; }

        public SettingsFields()
        {
            _startup = false;
            _displayNames = false;
            _themeOption = ThemeOption.Dark;
            _windowSizeOption = WindowSizeOption.Medium;
            _timeRanges = new List<TimeRange>();
        }

        public SettingsFields(bool startup, bool displayNames, ThemeOption theme, WindowSizeOption windowSizeOption)
        {
            _startup = startup;
            _displayNames = displayNames;
            _themeOption = theme;
            _windowSizeOption = windowSizeOption;
            _timeRanges = new List<TimeRange>();
        }

        public SettingsFields(bool startup, bool displayNames, ThemeOption theme, WindowSizeOption windowSizeOption, List<TimeRange> timeRanges)
        {
            _startup = startup;
            _displayNames = displayNames;
            _themeOption = theme;
            _windowSizeOption = windowSizeOption;
            _timeRanges = timeRanges ?? new List<TimeRange>();
        }
    }
}
