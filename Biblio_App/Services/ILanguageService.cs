using System;
using System.Globalization;

namespace Biblio_App.Services
{
    /// <summary>
    /// ILanguageService
    /// 
    /// Servicecontract voor taal- en cultuurbeheer binnen de MAUI app.
    /// 
    /// Deze service is verantwoordelijk voor:
    /// - het bijhouden van de huidige UI-cultuur
    /// - het wijzigen van de taal door de gebruiker
    /// - het terugzetten naar de toestel-/systeemtaal
    /// - het informeren van de UI wanneer de taal verandert
    /// 
    /// Wordt gebruikt door:
    /// - Pages
    /// - ViewModels
    /// - AppShell
    /// 
    /// zodat teksten live kunnen herladen zonder app-herstart.
    /// </summary>
    public interface ILanguageService
    {
        /// <summary>
        /// De momenteel actieve cultuur van de applicatie.
        /// 
        /// - Kan null zijn wanneer de app nog niet volledig geïnitialiseerd is
        /// - Wordt typisch gebruikt voor ResourceManager lookups
        /// </summary>
        CultureInfo? CurrentCulture { get; }

        /// <summary>
        /// Event dat wordt afgevuurd wanneer de taal wijzigt.
        /// 
        /// UI-componenten en ViewModels kunnen hierop subscriben
        /// om hun teksten opnieuw te laden.
        /// </summary>
        event EventHandler<CultureInfo>? LanguageChanged;

        /// <summary>
        /// Zet expliciet de applicatietaal.
        /// 
        /// De code is een ISO-taalcode, bv:
        /// - "nl"
        /// - "en"
        /// - "fr"
        /// 
        /// Implementaties kunnen deze keuze
        /// opslaan in SecureStorage of Preferences.
        /// </summary>
        void SetLanguage(string code);

        /// <summary>
        /// Reset de taalinstelling van de gebruiker.
        /// 
        /// - Verwijdert eventueel opgeslagen voorkeuren
        /// - Valt terug op de toestel-/systeemcultuur
        /// - Triggert LanguageChanged
        /// </summary>
        void ResetLanguage(); // clear saved preference and revert to device culture
    }
}
