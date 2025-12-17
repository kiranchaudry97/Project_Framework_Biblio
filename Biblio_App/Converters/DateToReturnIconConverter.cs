using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Converters
{
    public class DateToReturnIconConverter : IValueConverter
    {
        // geeft de bestandsnaam van het icoon terug afhankelijk van de status van de lening
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // If bound to the whole Lenen object
                if (value is Lenen lening)
                {
                    if (lening.ReturnedAt.HasValue)
                        return "checklist_ok_illustration.svg"; // ingeleverd

                    // overdue -> show red X
                    if (lening.DueDate < DateTime.Now.Date)
                        return "x_red_illustration.svg";

                    return "dotted3_illustration.svg"; // niet ingeleverd (nieuw symbool)
                }

                // If bound to a DateTime? (fallback)
                if (value == null || value == DBNull.Value) return "dotted3_illustration.svg"; // niet ingeleverd
                if (value is DateTime dt) return dt == default ? "dotted3_illustration.svg" : "checklist_ok_illustration.svg";

            }
            catch { }

            return "dotted3_illustration.svg";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // converter één-weg
            return Binding.DoNothing;
        }
    }
}
