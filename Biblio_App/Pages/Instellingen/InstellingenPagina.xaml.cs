using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System;
using Biblio_App.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using System.Linq;

namespace Biblio_App.Pages
{
    public partial class InstellingenPagina : ContentPage, ILocalizable
    {
        // ViewModel bevat o.a. sync acties en DB-info
        private InstellingenViewModel VM => BindingContext as InstellingenViewModel;

        // Service + ResourceManager voor taal/vertaling
        private ILanguageService? _language_service;
        private ResourceManager? _sharedResourceManager;

        public InstellingenPagina(InstellingenViewModel vm)
        {
            InitializeComponent();

            // MVVM: koppel ViewModel
            BindingContext = vm;

            try
            {
                // Shell navigatie/Back knop gedrag instellen (zelfde aanpak als andere pagina's)
                try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                try { Shell.SetFlyoutBehavior(this, FlyoutBehavior.Flyout); } catch { }
                try { NavigationPage.SetHasBackButton(this, false); } catch { }
            }
            catch { }

            try { _language_service = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }

            // ResourceManager initialiseren zodat Localize() kan werken
            InitializeSharedResourceManager();
            UpdateLocalizedStrings();
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                // Doel:
                // vertalingen ophalen via ResourceManager.
                // We proberen (in volgorde):
                // 1) Biblio_App resources
                // 2) Biblio_Web resources (handig tijdens dev)
                // 3) Biblio_Models resources (fallback)

                // 1) Prefer MAUI app resources first
                var appAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_App", StringComparison.OrdinalIgnoreCase));
                if (appAsm != null)
                {
                    foreach (var name in new[] { "Biblio_App.Resources.Vertalingen.SharedResource", "Biblio_App.Resources.SharedResource", "Biblio_App.SharedResource" })
                    {
                        try
                        {
                            var rm = new ResourceManager(name, appAsm);
                            var test = rm.GetString("Language", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
                                return;
                            }
                        }
                        catch { }
                    }
                }

                // 2) Then try web project resources
                var webAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (webAsm != null)
                {
                    foreach (var name in new[] { "Biblio_Web.Resources.Vertalingen.SharedResource", "Biblio_Web.Resources.SharedResource", "Biblio_Web.SharedResource" })
                    {
                        try
                        {
                            var rm = new ResourceManager(name, webAsm);
                            var test = rm.GetString("Language", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
                                return;
                            }
                        }
                        catch { }
                    }
                }

                var modelType = typeof(SharedModelResource);
                if (modelType != null)
                {
                    // 3) Fallback naar model resource
                    _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", modelType.Assembly);
                }
            }
            catch { }
        }

        private string Localize(string key)
        {
            try
            {
                // try AppShell helper first
                try
                {
                    var shell = AppShell.Instance;
                    if (shell != null)
                    {
                        var locMethod = typeof(AppShell).GetMethod("Localize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (locMethod != null)
                        {
                            try
                            {
                                var fromShell = locMethod.Invoke(shell, new object[] { key }) as string;
                                if (!string.IsNullOrEmpty(fromShell)) return fromShell;
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                var culture = _language_service?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                if (_sharedResourceManager != null)
                {
                    try
                    {
                        var val = _sharedResourceManager.GetString(key, culture);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    catch { }
                }

                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return key switch
                    {
                        "Settings" => "Settings",
                        "Language" => "Language",
                        "Sync" => "Synchronization",
                        "SyncNow" => "Sync now",
                        "CheckApi" => "Check API",
                        "Database" => "Database",
                        "LoadDbInfo" => "Load database info",
                        "ResetLocalDb" => "Reset local DB",
                        _ => key
                    };
                }

                if (code == "fr")
                {
                    return key switch
                    {
                        "Settings" => "Paramètres",
                        "Language" => "Langue",
                        "Sync" => "Synchronisation",
                        "SyncNow" => "Synchroniser maintenant",
                        "CheckApi" => "Vérifier l'API",
                        "Database" => "Base de données",
                        "LoadDbInfo" => "Charger les informations de la base",
                        "ResetLocalDb" => "Réinitialiser la base locale",
                        _ => key
                    };
                }

                // default NL
                return key switch
                {
                    "Settings" => "Instellingen",
                    "Language" => "Taal",
                    "Sync" => "Synchronisatie",
                    "SyncNow" => "Synchroniseer nu",
                    "CheckApi" => "Controleer API",
                    "Database" => "Database",
                    "LoadDbInfo" => "Laad database info",
                    "ResetLocalDb" => "Reset lokale DB",
                    _ => key
                };
            }
            catch { return key; }
        }

        public void UpdateLocalizedStrings()
        {
            try
            {
                var title = Localize("Settings");
                try { if (TitleLabel != null) TitleLabel.Text = title; } catch { }
                try { if (HeaderLabel != null) HeaderLabel.Text = title; } catch { }
                try { if (LanguageLabel != null) LanguageLabel.Text = Localize("Language"); } catch { }
                try { if (SyncLabel != null) SyncLabel.Text = Localize("Sync"); } catch { }
                try { if (SyncNowButton != null) SyncNowButton.Text = Localize("SyncNow"); } catch { }
                try { if (CheckApiButton != null) CheckApiButton.Text = Localize("CheckApi"); } catch { }
                try { if (DatabaseHeaderLabel != null) DatabaseHeaderLabel.Text = Localize("Database"); } catch { }
                try { if (LoadDbButton != null) LoadDbButton.Text = Localize("LoadDbInfo"); } catch { }
                try { if (ResetDbButton != null) ResetDbButton.Text = Localize("ResetLocalDb"); } catch { }
                // ResetLanguageButton was removed from the UI; no-op here
            }
            catch { }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Bij het openen van de instellingen:
            // - haal DB info op zodat je kan tonen waar de SQLite DB staat
            // - subscribe op taalwijzigingen zodat labels live kunnen updaten
            if (VM != null)
            {
                await VM.LoadDatabaseInfoAsync();
            }

            try
            {
                if (_language_service != null)
                {
                    _language_service.LanguageChanged += LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (_language_service != null)
                {
                    _language_service.LanguageChanged -= LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
            }
            catch { }
        }

        private async void OnResetDbClicked(object sender, EventArgs e)
        {
            // Reset lokale DB:
            // 1) vraag bevestiging (zodat gebruiker niet per ongeluk alles wist)
            // 2) laat ViewModel DB verwijderen + opnieuw seeden
            // 3) toon resultaat
            var ok = await DisplayAlert(Localize("Settings"), Localize("ResetLocalDb") + "?", Localize("Yes"), Localize("No"));
            if (!ok) return;

            if (VM != null)
            {
                var res = await VM.ResetAndSeedLocalDatabaseAsync();
                await DisplayAlert(Localize("Settings"), res ? Localize("ResetLocalDb") + " done." : "Could not reset database.", Localize("OK"));
            }
        }

        private void OnResetLanguageClicked(object sender, EventArgs e)
        {
            try
            {
                _language_service?.ResetLanguage();
            }
            catch { }
        }
    }
}
