using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Biblio_App.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            // If value is null or not bool, treat as false -> inverse true
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return false;
        }
    }
}
