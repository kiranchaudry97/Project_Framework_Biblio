namespace Biblio_App.Services
{
    /// <summary>
    /// ILocalizable
    /// 
    /// Eenvoudig marker-/servicecontract voor pagina’s en ViewModels
    /// die hun gelokaliseerde UI-teksten dynamisch kunnen bijwerken.
    /// 
    /// Dit interface wordt gebruikt door:
    /// - AppShell
    /// - Pages (ContentPage)
    /// - ViewModels
    /// 
    /// zodat bij een taalwijziging (via ILanguageService)
    /// de UI onmiddellijk kan worden hertekend
    /// zonder de applicatie opnieuw te starten.
    /// </summary>
    public interface ILocalizable
    {
        /// <summary>
        /// Methode die alle gelokaliseerde strings
        /// (labels, titels, placeholders, knoppen, …)
        /// opnieuw instelt op basis van de huidige cultuur.
        /// 
        /// Wordt typisch aangeroepen wanneer:
        /// - de gebruiker van taal wisselt
        /// - de pagina opnieuw verschijnt
        /// - AppShell een globale refresh uitvoert
        /// </summary>
        void UpdateLocalizedStrings();
    }
}
