using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Biblio_App.Converters
{
    /// <summary>
    /// Converter that checks if a value equals another value (parameter).
    /// Returns true if equal, false otherwise.
    /// </summary>
    public class EqualToConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null)
                return true;

            if (value == null || parameter == null)
                return false;

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EqualToConverter does not support ConvertBack");
        }
    }
}
