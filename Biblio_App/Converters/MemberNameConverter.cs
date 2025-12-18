using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Converters
{
    public class MemberNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Lid l)
                {
                    var first = l.Voornaam ?? string.Empty;
                    var last = l.AchterNaam ?? string.Empty;
                    var full = (first + " " + last).Trim();
                    return string.IsNullOrWhiteSpace(full) ? "-" : full;
                }

                // If binding passes the whole object as '.' this will be handled above
                // Fallback to ToString()
                return value?.ToString() ?? string.Empty;
            }
            catch
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
