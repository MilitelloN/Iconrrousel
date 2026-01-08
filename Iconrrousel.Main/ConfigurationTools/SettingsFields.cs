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

        public enum IconSizeOption
        {
            Small,
            Medium,
            Large
        }

        public ThemeOption _themeOption { get; set; }
        public bool _startup { get; set; }
        public bool _displayNames { get; set; }
        public IconSizeOption _iconSizeOption { get; set; }

        public SettingsFields()
        {
            _startup = false;
            _displayNames = false;
            _themeOption = ThemeOption.Dark;
        }

        public SettingsFields(bool startup, bool displayNames, ThemeOption theme, IconSizeOption iconSizeOption)
        {
            _startup = startup;
            _displayNames = displayNames;
            _themeOption = theme;
            _iconSizeOption = iconSizeOption;
        }




        
    }
}
