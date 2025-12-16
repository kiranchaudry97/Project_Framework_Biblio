using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System.Linq;
using System;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using Biblio_App.Services;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;

namespace Biblio_App.Pages
{
    public partial class BoekenPagina : ContentPage, ILocalizable
    {
        private BoekenViewModel VM => BindingContext as BoekenViewModel;
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public BoekenPagina(BoekenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            if (vm != null)
                vm.PropertyChanged += Vm_PropertyChanged;

            // resolve language service if possible
            try
            {
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
                // prefer AppShell translation when available
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

                // default nl
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

            // Zorg dat categorieën en boeken geladen zijn wanneer de pagina verschijnt zodat de categorie-picker alle items toont
            if (VM != null)
            {
                await VM.EnsureCategoriesLoadedAsync();

                // als er geen geselecteerde filter is, kies de eerste (meestal 'Alle')
                if (VM.SelectedFilterCategorie == null && VM.Categorien?.Count > 0)
                {
                    VM.SelectedFilterCategorie = VM.Categorien.FirstOrDefault();
                }
            }

            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged -= LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BoekenViewModel.HasValidationErrors) || e.PropertyName == nameof(BoekenViewModel.ValidationMessage))
            {
                FocusFirstError();
            }

            if (e.PropertyName == nameof(BoekenViewModel.SelectedBoek))
            {
                if (VM?.SelectedBoek == null)
                {
                    TitelEntry?.Focus();
                }
            }

            // ensure title label reflects changes to PageTitle property on the ViewModel
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
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                {
                    // Set the selected item on the VM so form fields are populated
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
                    try
                    {
                        if (VM != null)
                        {
                            VM.SelectedBoek = boek;
                        }
                    }
                    catch { }
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

                        // Explicitly refresh page title display after viewmodel updated
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
                if (System.Diagnostics.Debugger.IsAttached)
                {
                  
                        System.Diagnostics.Debugger.Break();
                }
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

                // Update the Bindable Title property (so Shell.Title/Backstack reflect it)
                try { this.Title = title; } catch { }

                // Update the in-TitleView Label explicitly (XAML binding may not refresh in some platforms)
                try { if (TitleLabel != null) TitleLabel.Text = title; } catch { }
            }
            catch { }
        }
    }
}
