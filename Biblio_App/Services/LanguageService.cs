using System;
using System.Globalization;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Biblio_Models.Resources;
using Microsoft.Maui.Controls;

namespace Biblio_App.Services
{
    /// <summary>
    /// LanguageService
    /// 
    /// Centrale service die verantwoordelijk is voor:
    /// - het instellen van de huidige taal (CultureInfo)
    /// - het bewaren van de taalvoorkeur in Preferences
    /// - het informeren van de applicatie wanneer de taal wijzigt
    /// 
    /// Deze service wordt gebruikt door:
    /// - AppShell
    /// - Pages (ContentPage)
    /// - ViewModels
    /// 
    /// via het ILanguageService + ILocalizable patroon.
    /// </summary>
    public class LanguageService : ILanguageService
    {
        /// <summary>
        /// De huidige cultuur van de applicatie.
        /// Wordt gebruikt door ResourceManagers en UI bindingen.
        /// </summary>
        public CultureInfo? CurrentCulture { get; private set; }

        /// <summary>
        /// Event dat wordt afgevuurd wanneer de taal wijzigt.
        /// Pagina’s en ViewModels kunnen zich hierop abonneren
        /// om hun UI dynamisch te herladen.
        /// </summary>
        public event EventHandler<CultureInfo>? LanguageChanged;

        /// <summary>
        /// Constructor
        /// 
        /// Bij het opstarten van de app:
        /// - controleren we of de gebruiker eerder een taal koos
        /// - zo ja: laden we die uit Preferences
        /// - stellen we de globale CultureInfo correct in
        /// </summary>
        public LanguageService()
        {
            try
            {
                // Controleer of er een opgeslagen taalvoorkeur bestaat
                if (Preferences.Default.ContainsKey("biblio-culture"))
                {
                    var code = Preferences.Default.Get("biblio-culture", string.Empty);

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        // Maak een CultureInfo object aan op basis van de code (bv. "nl", "en", "fr")
                        var culture = new CultureInfo(code);

                        // Zet de huidige cultuur van de service
                        CurrentCulture = culture;

                        // Stel de cultuur in voor alle threads (UI + background)
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;

                        // Synchroniseer ook de gedeelde model-resources
                        try { SharedModelResource.Culture = culture; } catch { }

#if DEBUG
                        // Debug logging (enkel in DEBUG builds)
                        try
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"LanguageService ctor: applied saved culture {culture.Name}");
                        }
                        catch { }
#endif
                    }
                }
            }
            catch
            {
                // Fouten bij Preferences of CultureInfo mogen de app niet laten crashen
            }
        }

        /// <summary>
        /// Zet expliciet een nieuwe taal in de applicatie.
        /// 
        /// Deze methode:
        /// - stelt de nieuwe cultuur in
        /// - bewaart de voorkeur in Preferences
        /// - triggert het LanguageChanged event
        /// </summary>
        /// <param name="code">Taalcode (bv. "nl", "en", "fr")</param>
        public void SetLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            try
            {
#if DEBUG
                // Debug logging + breakpoint voor ontwikkelaars
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"LanguageService.SetLanguage called with code='{code}'");
                }
                catch { }

                try
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debugger.Break();
                }
                catch { }
#endif
                // Nieuwe cultuur aanmaken
                var culture = new CultureInfo(code);

                // Huidige cultuur bijwerken
                CurrentCulture = culture;

                // Globale cultuur instellen voor alle threads
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                // Taalvoorkeur opslaan zodat ze behouden blijft na herstart
                try { Preferences.Default.Set("biblio-culture", code); } catch { }

                // Ook de gedeelde model-resources updaten
                try { SharedModelResource.Culture = culture; } catch { }

                // Abonnees verwittigen (AppShell, pagina’s, viewmodels)
                LanguageChanged?.Invoke(this, culture);

#if DEBUG
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"LanguageService.SetLanguage: invoked LanguageChanged for {culture.Name}");
                }
                catch { }
#endif

                /*
                 * BELANGRIJK:
                 * 
                 * We maken hier GEEN nieuwe MainPage aan.
                 * 
                 * Het heropbouwen van Application.Current.MainPage
                 * kan leiden tot:
                 * - dubbele pagina-initialisatie
                 * - PlatformView null-exceptions
                 * - threading issues (Android / iOS)
                 * 
                 * AppShell en ILocalizable handlers zorgen zelf
                 * voor het correct verversen van de UI.
                 */
            }
            catch
            {
                // Fouten bij cultuurwissel worden genegeerd om crashes te voorkomen
            }
        }

        /// <summary>
        /// Reset de taalinstelling:
        /// - verwijdert de opgeslagen voorkeur
        /// - schakelt terug naar de device-cultuur
        /// - verwittigt alle subscribers
        /// </summary>
        public void ResetLanguage()
        {
            try
            {
                // Verwijder opgeslagen voorkeur
                try { Preferences.Default.Remove("biblio-culture"); } catch { }

                // Gebruik toestelcultuur als fallback
                var deviceCulture =
                    CultureInfo.CurrentCulture
                    ?? CultureInfo.CurrentUICulture
                    ?? new CultureInfo("en");

                // Update huidige cultuur
                CurrentCulture = deviceCulture;

                // Globale cultuur instellen
                CultureInfo.DefaultThreadCurrentCulture = deviceCulture;
                CultureInfo.DefaultThreadCurrentUICulture = deviceCulture;

                // Update gedeelde model-resources
                try { SharedModelResource.Culture = deviceCulture; } catch { }

                // Informeer UI en ViewModels
                LanguageChanged?.Invoke(this, deviceCulture);

#if DEBUG
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"LanguageService.ResetLanguage: reset to device culture {deviceCulture.Name}");
                }
                catch { }
#endif
            }
            catch
            {
                // Geen crash bij resetproblemen
            }
        }
    }
}
