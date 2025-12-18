using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Converters
{
    public class DateToReturnLabelConverter : IValueConverter
    {
        private string Localize(string key)
        {
            try
            {
                var shell = AppShell.Instance;
                if (shell != null)
                {
                    try
                    {
                        var val = shell.Translate(key);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    catch { }
                }
            }
            catch { }

            var culture = CultureInfo.CurrentUICulture;
            var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (code == "en")
            {
                return key switch
                {
                    "ReturnedLabel" => "Return status",
                    "ReturnedOption" => "Returned",
                    "Return" => "Return",
                    "Late" => "Late",
                    _ => key
                };
            }

            if (code == "fr")
            {
                return key switch
                {
                    "ReturnedLabel" => "Statut de livraison",
                    "ReturnedOption" => "Rendu",
                    "Return" => "Retourner",
                    "Late" => "En retard",
                    _ => key
                };
            }

            return key switch
            {
                "ReturnedLabel" => "Lever status",
                "ReturnedOption" => "Geleverd",
                "Return" => "Inleveren",
                "Late" => "Te laat",
                _ => key
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Lenen l)
                {
                    if (l.ReturnedAt.HasValue) return Localize("ReturnedOption");

                    // explicit UI overrides
                    if (l.ForceNotLate) return Localize("Return");
                    if (l.ForceLate) return Localize("Late");

                    // overdue by date
                    if (l.DueDate.Date < DateTime.Today) return Localize("Late");

                    return Localize("Return");
                }

                if (value == null || value == DBNull.Value) return Localize("Return");
                if (value is DateTime dt)
                {
                    if (dt == default) return Localize("Return");
                    return dt.Date < DateTime.Today ? Localize("Late") : Localize("Return");
                }
            }
            catch { }

            return Localize("Return");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
