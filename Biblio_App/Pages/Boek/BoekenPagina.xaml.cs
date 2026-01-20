using Biblio_App.Services;
using Biblio_App.ViewModels;
using Biblio_Models.Resources;
using System.Globalization;
using System.Resources;

namespace Biblio_App.Pages
{
    public partial class BoekenPagina : ContentPage, ILocalizable
    {
        // Short-hand naar de ViewModel die aan de pagina hangt
        private BoekenViewModel VM => BindingContext as BoekenViewModel;

        // Taalservice + resource manager om de teksten in de UI te vertalen
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public BoekenPagina(BoekenViewModel vm)
        {
            InitializeComponent();

            // MVVM: ViewModel koppelen aan de pagina
            BindingContext = vm;
            if (vm != null)
                vm.PropertyChanged += Vm_PropertyChanged;

            try
            {
                // Navigatie/Back-knop gedrag aanpassen zodat Shell menu correct werkt
                try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                try { Shell.SetFlyoutBehavior(this, FlyoutBehavior.Flyout); } catch { }
                try { NavigationPage.SetHasBackButton(this, false); } catch { }
            }
            catch { }

            try
            {
                // Services uit DI halen (MauiContext) - kan null zijn in design-time
                _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                InitializeSharedResourceManager();
            }
            catch { }
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                var webAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (webAsm != null)
                {
                    foreach (var name in new[] { "Biblio_Web.Resources.Vertalingen.SharedResource", "Biblio_Web.Resources.SharedResource", "Biblio_Web.SharedResource" })
                    {
                        try
                        {
                            var rm = new ResourceManager(name, webAsm);
                            var test = rm.GetString("Details", CultureInfo.CurrentUICulture);
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
                    _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", modelType.Assembly);
                }
            }
            catch { }
        }

        private string Localize(string key)
        {
            try
            {
                var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                // geef voorkeur aan AppShell vertaling wanneer beschikbaar
                try
                {
                    var shell = AppShell.Instance;
                    if (shell != null)
                    {
                        var val = shell.Translate(key);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }
                catch { }

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
                        "Details" => "Details",
                        "OK" => "OK",
                        "ISBN" => "ISBN",
                        "Category" => "Category",
                        _ => key
                    };
                }

                if (code == "fr")
                {
                    return key switch
                    {
                        "Details" => "Détails",
                        "OK" => "OK",
                        "ISBN" => "ISBN",
                        "Category" => "Catégorie",
                        _ => key
                    };
                }

                // standaard nl
                return key switch
                {
                    "Details" => "Details",
                    "OK" => "OK",
                    "ISBN" => "ISBN",
                    "Category" => "Categorie",
                    _ => key
                };
            }
            catch { return key; }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Wanneer de pagina zichtbaar wordt:
            // - roepen we InitializeAsync op (laadt boeken/categorieën/tellers)
            // - we abonneren op taalwijzigingen zodat labels live kunnen updaten
            try
            {
                if (BindingContext is BoekenViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BoekenPagina.OnAppearing InitializeAsync error: {ex}");
            }

            if (VM != null)
            {
                // Best-effort categorieën ophalen (async), zodat filter/picker niet leeg is
                _ = LoadCategoriesAsync();
            }

            try
            {
                if (_languageService != null)
                {
                    // Als de gebruiker van taal wisselt -> UI strings opnieuw zetten
                    _languageService.LanguageChanged += LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var vm = VM;
                if (vm == null) return;

                // Load categories (this runs async already)
                await vm.EnsureCategoriesLoadedAsync();

                // Update UI on main thread
                if (VM != null && VM.SelectedFilterCategorie == null && VM.Categorien?.Count > 0)
                {
                    // Default filter = eerste item (meestal "Alle")
                    VM.SelectedFilterCategorie = VM.Categorien.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (_languageService != null)
                {
                    // Unsubscribe om memory leaks te vermijden
                    _languageService.LanguageChanged -= LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BoekenViewModel.HasValidationErrors) || e.PropertyName == nameof(BoekenViewModel.ValidationMessage))
            {
                // Als validatie faalt, zetten we focus op het eerste fout veld
                FocusFirstError();
            }

            if (e.PropertyName == nameof(BoekenViewModel.SelectedBoek))
            {
                if (VM?.SelectedBoek == null)
                {
                    TitelEntry?.Focus();
                }
            }

            if (e.PropertyName == nameof(BoekenViewModel.PageTitle))
            {
                RefreshTitleFromViewModel();
            }
        }

        private void FocusFirstError()
        {
            if (VM == null) return;
            if (!string.IsNullOrEmpty(VM.TitelError))
            {
                TitelEntry?.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.AuteurError))
            {
                AuteurEntry?.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.IsbnError))
            {
                IsbnEntry?.Focus();
                return;
            }
        }

        // Nieuwe click-handlers voor de afbeeldingsknoppen
        private async void OnDetailsClicked(object sender, EventArgs e)
        {
            // "Details" knop in de lijst: selecteer boek in de viewmodel
            // zodat het edit/detail paneel ingevuld wordt.
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            if (VM != null)
                            {
                                VM.SelectedBoek = boek;
                                return;
                            }
                        }
                        catch { }

                        // fallback: navigeer naar de detailpagina of toon een melding met basisinformatie
                        var title = Localize("Details");
                        var okText = Localize("OK");
                        var isbnLabel = Localize("ISBN");
                        var body = $"{boek.Titel}\n{boek.Auteur}\n{isbnLabel}: {boek.Isbn}";
                        await DisplayAlert(title, body, okText);
                    });
                }
            }
            catch { }
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            if (VM != null)
                            {
                                VM.SelectedBoek = boek;
                            }
                        }
                        catch { }
                    });
                }
            }
            catch { }
        }

        private async void OnCreateNewBookClicked(object sender, EventArgs e)
        {
            try
            {
                // navigeer naar de aanmaakpagina via de geregistreerde route (typename gebruikt voor nameof)
                await Shell.Current.GoToAsync(nameof(Biblio_App.Pages.Boek.BoekCreatePage));
            }
            catch { }
        }

        // Toon volledige tekst wanneer op een afgeknot label wordt getapt
        private async void OnLabelTapped(object sender, EventArgs e)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (sender is Label lbl)
                    {
                        var text = lbl.Text;
                        // Probeer meer context uit BindingContext te halen indien beschikbaar (boek)
                        if (lbl.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                        {
                            // bepaal welke eigenschap is aangeraakt op basis van de kolom (gebruik index van parent grid children)
                            // fallback: toon titel + auteur + isbn
                            var isbnLabel = Localize("ISBN");
                            var categoryLabel = Localize("Category");
                            text = $"{boek.Titel}\n{boek.Auteur}\n{isbnLabel}: {boek.Isbn}\n{categoryLabel}: {boek.CategorieID}";
                        }

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var title = Localize("Details");
                            var okText = Localize("OK");
                            await DisplayAlert(title, text, okText);
                        }
                    }
                });
            }
            catch { }
        }

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        try
                        {
                            if (VM is Biblio_App.Services.ILocalizable loc)
                            {
                                loc.UpdateLocalizedStrings();
                            }
                            else
                            {
                                VM?.RefreshLocalizedStrings();
                            }
                        }
                        catch { }

                        try { this.ForceLayout(); this.InvalidateMeasure(); }
                        catch { }

                        try { RefreshTitleFromViewModel(); } catch { }
                    }
                    catch { }
                });
            }
            catch { }
        }

        public void UpdateLocalizedStrings()
        {
#if DEBUG
            try
            {
                System.Diagnostics.Debug.WriteLine("BoekenPagina.UpdateLocalizedStrings called");
            }
            catch { }
#endif
            try
            {
                if (VM is Biblio_App.Services.ILocalizable locVm)
                {
                    try { locVm.UpdateLocalizedStrings(); } catch { }
                }
                else
                {
                    try { VM?.RefreshLocalizedStrings(); } catch { }
                }

                try
                {
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { this.ForceLayout(); this.InvalidateMeasure(); }
                        catch { }

                        try { RefreshTitleFromViewModel(); } catch { }
                    });
                }
                catch { }
            }
            catch { }
        }

        private void RefreshTitleFromViewModel()
        {
            try
            {
                if (VM == null) return;
                var title = VM.PageTitle ?? string.Empty;

                try { this.Title = title; } catch { }
            }
            catch { }
        }
    }
}
