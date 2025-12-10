using System;
using System.Globalization;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Biblio_Models.Resources;
using Microsoft.Maui.Controls;

namespace Biblio_App.Services
{
    public class LanguageService : ILanguageService
    {
        public CultureInfo? CurrentCulture { get; private set; }

        public event EventHandler<CultureInfo>? LanguageChanged;

        public LanguageService()
        {
            try
            {
                if (Preferences.Default.ContainsKey("biblio-culture"))
                {
                    var code = Preferences.Default.Get("biblio-culture", string.Empty);
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        var culture = new CultureInfo(code);
                        CurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;

                        // informeer ook de gedeelde modelresources
                        try { SharedModelResource.Culture = culture; } catch { }
                    }
                }
            }
            catch { }
        }

        public void SetLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            try
            {
                var culture = new CultureInfo(code);
                CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                try { Preferences.Default.Set("biblio-culture", code); } catch { }

                // werk de cultuur van SharedModelResource bij zodat resource-opzoekingen de nieuwe cultuur gebruiken
                try { SharedModelResource.Culture = culture; } catch { }

                LanguageChanged?.Invoke(this, culture);

                // Do NOT recreate the main page here. AppShell (and other subscribers) will handle UI refresh after the LanguageChanged event.
                // Recreating Application.Current.MainPage from here can cause re-entrant construction and platform handler/PlatformView null exceptions on some platforms.
            }
            catch
            {
                // negeren bij fout
            }
        }
    }
}
