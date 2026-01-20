using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using Microsoft.Extensions.DependencyInjection;
using Biblio_App.Services;
using System;

namespace Biblio_App.Pages.Boek
{
    public partial class BoekCreatePage : ContentPage, ILocalizable
    {
        // Deze pagina gebruikt hetzelfde `BoekenViewModel` als de boekenlijst,
        // maar toont enkel het "nieuw boek" formulier.
        private BoekenViewModel VM => BindingContext as BoekenViewModel;

        // Taalservice + resources voor vertaling
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(BoekCreatePage), default(string));
        public static readonly BindableProperty TitelPlaceholderProperty = BindableProperty.Create(nameof(TitelPlaceholder), typeof(string), typeof(BoekCreatePage), default(string));
        public static readonly BindableProperty AuteurPlaceholderProperty = BindableProperty.Create(nameof(AuteurPlaceholder), typeof(string), typeof(BoekCreatePage), default(string));
        public static readonly BindableProperty IsbnPlaceholderProperty = BindableProperty.Create(nameof(IsbnPlaceholder), typeof(string), typeof(BoekCreatePage), default(string));
        public static readonly BindableProperty CategoryTitleProperty = BindableProperty.Create(nameof(CategoryTitle), typeof(string), typeof(BoekCreatePage), default(string));
        public static readonly BindableProperty CancelButtonTextProperty = BindableProperty.Create(nameof(CancelButtonText), typeof(string), typeof(BoekCreatePage), default(string));
        public static readonly BindableProperty SaveButtonTextProperty = BindableProperty.Create(nameof(SaveButtonText), typeof(string), typeof(BoekCreatePage), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string TitelPlaceholder { get => (string)GetValue(TitelPlaceholderProperty); set => SetValue(TitelPlaceholderProperty, value); }
        public string AuteurPlaceholder { get => (string)GetValue(AuteurPlaceholderProperty); set => SetValue(AuteurPlaceholderProperty, value); }
        public string IsbnPlaceholder { get => (string)GetValue(IsbnPlaceholderProperty); set => SetValue(IsbnPlaceholderProperty, value); }
        public string CategoryTitle { get => (string)GetValue(CategoryTitleProperty); set => SetValue(CategoryTitleProperty, value); }
        public string CancelButtonText { get => (string)GetValue(CancelButtonTextProperty); set => SetValue(CancelButtonTextProperty, value); }
        public string SaveButtonText { get => (string)GetValue(SaveButtonTextProperty); set => SetValue(SaveButtonTextProperty, value); }

        public BoekCreatePage(BoekenViewModel vm)
        {
            InitializeComponent();

            // MVVM: ViewModel koppelen aan de pagina
            BindingContext = vm;

            try { _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }
            InitializeSharedResourceManager();

            // UI teksten initialiseren volgens huidige taal
            UpdateLocalizedStrings();
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                // Zelfde resource strategie als andere pagina's:
                // 1) web resources (als beschikbaar)
                // 2) model resources (fallback)
                var webAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (webAsm != null)
                {
                    foreach (var name in new[] { "Biblio_Web.Resources.Vertalingen.SharedResource", "Biblio_Web.Resources.SharedResource", "Biblio_Web.SharedResource" })
                    {
                        try
                        {
                            var rm = new ResourceManager(name, webAsm);
                            var test = rm.GetString("Boeken", CultureInfo.CurrentUICulture);
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
                // Localize helper:
                // - probeer ResourceManager (resx)
                // - anders hard-coded fallback
                var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                if (_sharedResourceManager != null)
                {
                    var val = _sharedResourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(val)) return val;
                }

                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return key switch
                    {
                        "Boeken" => "Books",
                        "Titel" => "Title",
                        "Auteur" => "Author",
                        "ISBN" => "ISBN",
                        "Categorie" => "Category",
                        "Annuleer" => "Cancel",
                        "Opslaan" => "Save",
                        _ => key
                    };
                }

                return key switch
                {
                    "Boeken" => "Nieuw boek",
                    "Titel" => "Titel",
                    "Auteur" => "Auteur",
                    "ISBN" => "ISBN",
                    "Categorie" => "Categorie",
                    "Annuleer" => "Annuleer",
                    "Opslaan" => "Opslaan",
                    _ => key
                };
            }
            catch { return key; }
        }

        public void UpdateLocalizedStrings()
        {
            // Zet alle bindable properties voor labels/placeholders/knoppen
            PageHeaderText = Localize("Boeken");
            TitelPlaceholder = Localize("Titel");
            AuteurPlaceholder = Localize("Auteur");
            IsbnPlaceholder = Localize("ISBN");
            CategoryTitle = Localize("Categorie");
            CancelButtonText = Localize("Annuleer");
            SaveButtonText = Localize("Opslaan");
        }

        public void ApplyQueryAttributes(System.Collections.Generic.IDictionary<string, object> query)
        {
            // Shell query parameters:
            // als we een `boekId` meekrijgen, selecteren we dat boek in de ViewModel.
            // Handig als we dezelfde pagina ook zouden gebruiken voor "edit".
            if (query == null) return;
            if (query.TryGetValue("boekId", out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out var id))
                {
                    var vm = BindingContext as BoekenViewModel;
                    if (vm != null)
                    {
                        var b = vm.Boeken.FirstOrDefault(x => x.Id == id);
                        if (b != null) vm.SelectedBoek = b;
                        else
                        {
                            _ = vm.EnsureCategoriesLoadedAsync();
                            var found = vm.Boeken.FirstOrDefault(x => x.Id == id);
                            if (found != null) vm.SelectedBoek = found;
                        }
                    }
                }
            }
        }

        private async void OnCancelClicked(object sender, System.EventArgs e)
        {
            // Cancel: ga één pagina terug in de Shell navigatie stack
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
