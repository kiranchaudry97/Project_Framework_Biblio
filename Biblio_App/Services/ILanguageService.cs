using System;
using System.Globalization;

namespace Biblio_App.Services
{
    public interface ILanguageService
    {
        CultureInfo? CurrentCulture { get; }
        event EventHandler<CultureInfo>? LanguageChanged;
        void SetLanguage(string code);
        void ResetLanguage(); // clear saved preference and revert to device culture
    }
}
