using System;
using System.Globalization;
using System.Windows.Data;

namespace Iconrrousel.Main.Converters
{
    public class ShowNamesToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool showNames = value is bool b && b;
            return showNames ? UIConfiguration.Icon.ImageHeightWithLabel : UIConfiguration.Icon.ImageHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
