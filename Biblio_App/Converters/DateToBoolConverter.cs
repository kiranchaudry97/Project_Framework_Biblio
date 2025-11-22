using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Biblio_App.Converters
{
    public class DateToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is DateTime dt) return dt != default;
            if (value is DateTime?) return ((DateTime?)value).HasValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return DateTime.Now;
            return null;
        }
    }
}
