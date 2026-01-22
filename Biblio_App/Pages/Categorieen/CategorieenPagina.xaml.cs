using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using Microsoft.Maui.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Biblio_App.Services;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using System.Linq;

namespace Biblio_App.Pages
{
    public partial class CategorieenPagina : ContentPage, ILocalizable
    {
        // Short-hand naar de gekoppelde ViewModel (MVVM)
        private CategorieenViewModel VM => BindingContext as CategorieenViewModel;

        // Service + ResourceManager voor taal/vertaling
        private ILanguageService? _language_service;
        private ResourceManager? _sharedResourceManager;

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(CategorieenPagina), default(string));
        public static readonly BindableProperty NamePlaceholderProperty = BindableProperty.Create(nameof(NamePlaceholder), typeof(string), typeof(CategorieenPagina), default(string));
        public static readonly BindableProperty NewButtonTextProperty = BindableProperty.Create(nameof(NewButtonText), typeof(string), typeof(CategorieenPagina), default(string));
        public static readonly BindableProperty SaveButtonTextProperty = BindableProperty.Create(nameof(SaveButtonText), typeof(string), typeof(CategorieenPagina), default(string));
        public static readonly BindableProperty DeleteButtonTextProperty = BindableProperty.Create(nameof(DeleteButtonText), typeof(string), typeof(CategorieenPagina), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string NamePlaceholder { get => (string)GetValue(NamePlaceholderProperty); set => SetValue(NamePlaceholderProperty, value); }
        public string NewButtonText { get => (string)GetValue(NewButtonTextProperty); set => SetValue(NewButtonTextProperty, value); }
        public string SaveButtonText { get => (string)GetValue(SaveButtonTextProperty); set => SetValue(SaveButtonTextProperty, value); }
        public string DeleteButtonText { get => (string)GetValue(DeleteButtonTextProperty); set => SetValue(DeleteButtonTextProperty, value); }

        public CategorieenPagina(CategorieenViewModel vm)
        {
            InitializeComponent();

            // MVVM: ViewModel koppelen zodat XAML bindings werken
            BindingContext = vm;

            try
            {
                // Shell navigatie/Back knop gedrag instellen (zelfde aanpak als andere pagina's)
                try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                try { Shell.SetFlyoutBehavior(this, FlyoutBehavior.Flyout); } catch { }
                try { NavigationPage.SetHasBackButton(this, false); } catch { }
            }
            catch { }

            // LanguageService uit DI halen (kan null zijn in design-time)
            try { _language_service = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }

            InitializeSharedResourceManager();

            // Teksten initialiseren (title/knoppen/placeholders) volgens de huidige taal
            UpdateLocalizedStrings();
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                // Doel:
                // vertalingen ophalen via resx/ResourceManager.
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
                            var test = rm.GetString("Categories", CultureInfo.CurrentUICulture);
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
                            // Test key om te checken of deze resx effectief bestaat
                            var rm = new ResourceManager(name, webAsm);
                            var test = rm.GetString("Categories", CultureInfo.CurrentUICulture);
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
                    // 3) Laatste fallback: gedeelde resource uit het model-project
                    _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", modelType.Assembly);
                }
            }
            catch { }
        }

        private string Localize(string key)
        {
            try
            {
                // Vertaling helper:
                // 1) AppShell.Localize (centrale plek zodat menu + pagina's consistent vertalen)
                // 2) ResourceManager (resx)
                // 3) hard-coded fallback per taal

                // 1) try AppShell helper first
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

                // 2) ResourceManager lookup
                var culture = _language_service?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                if (_sharedResourceManager != null)
                {
                    var val = _sharedResourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(val)) return val;
                }

                // 3) Fallback strings (voor het geval de resx key ontbreekt)
                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return key switch
                    {
                        "Categories" => "Categories",
                        "Name" => "Name",
                        "New" => "New",
                        "Save" => "Save",
                        "Delete" => "Delete",
                        _ => key
                    };
                }

                if (code == "fr")
                {
                    return key switch
                    {
                        "Categories" => "Catégories",
                        "Name" => "Nom",
                        "New" => "Nouveau",
                        "Save" => "Enregistrer",
                        "Delete" => "Supprimer",
                        _ => key
                    };
                }

                return key switch
                {
                    "Categories" => "Categorieen",
                    "Name" => "Naam",
                    "New" => "Nieuw",
                    "Save" => "Opslaan",
                    "Delete" => "Verwijder",
                    _ => key
                };
            }
            catch { return key; }
        }

        public void UpdateLocalizedStrings()
        {
            // Update UI teksten op de pagina.
            // Dit wordt gebruikt bij:
            // - constructor (init)
            // - taalwijziging (LanguageChanged event)
            try
            {
                if (VM is Biblio_App.Services.ILocalizable locVm)
                {
                    // Ook de ViewModel heeft gelokaliseerde teksten, dus die updaten we ook
                    try { locVm.UpdateLocalizedStrings(); } catch { }
                }
            }
            catch { }

            PageHeaderText = Localize("Categories");
            NamePlaceholder = Localize("Name");
            NewButtonText = Localize("New");
            SaveButtonText = Localize("Save");
            DeleteButtonText = Localize("Delete");

            try { RefreshTitleFromViewModel(); } catch { }
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Deze methode draait telkens wanneer de pagina zichtbaar wordt.
            // We doen hier:
            // 1) ViewModel initialiseren (laadt categorieën uit lokale DB)
            // 2) subscriben op taalwijzigingen
            // try/catch zodat de app niet crasht bij DB/IO problemen.
            try
            {
                if (BindingContext is CategorieenViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CategorieenPagina.OnAppearing InitializeAsync error: {ex}");
            }
            
            // Abonneer op taalwijzigingen zodat labels live kunnen updaten
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

            // Unsubscribe om memory leaks te vermijden
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
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    // UI teksten opnieuw instellen na taalwijziging
                    UpdateLocalizedStrings();
                    try { RefreshTitleFromViewModel(); } catch { }
                });
            }
            catch { }
        }

        private void RefreshTitleFromViewModel()
        {
            try
            {
                var title = PageHeaderText ?? string.Empty;
                try { this.Title = title; } catch { }
                try { if (TitleLabel != null) TitleLabel.Text = title; } catch { }
            }
            catch { }
        }
    }
}
