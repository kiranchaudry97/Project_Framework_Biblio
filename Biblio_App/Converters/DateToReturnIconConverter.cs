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
                if (value is Lenen lening)
                {
                    if (lening.ReturnedAt.HasValue)
                        return "checklist_ok_illustration.svg"; // ingeleverd

                    if (lening.ForceNotLate)
                        return "dotted3_illustration.svg";

                    if (lening.ForceLate || lening.DueDate.Date < DateTime.Today)
                        return "x_red_illustration.svg";

                    return "dotted3_illustration.svg"; // niet ingeleverd (nieuw symbool)
                }

                if (value == null || value == DBNull.Value) return "dotted3_illustration.svg"; // niet ingeleverd
                if (value is DateTime dt)
                {
                    if (dt == default) return "dotted3_illustration.svg";
                    if (dt.Date < DateTime.Today) return "x_red_illustration.svg";
                    return "checklist_ok_illustration.svg";
                }

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
