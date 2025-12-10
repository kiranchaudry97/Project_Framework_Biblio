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
    public partial class CategorieenPagina : ContentPage
    {
        private CategorieenViewModel VM => BindingContext as CategorieenViewModel;
        private ILanguageService? _languageService;
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
            BindingContext = vm;

            try { _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }

            InitializeSharedResourceManager();
            UpdateLocalizedStrings();

            try
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += OnLanguageLabelTapped;
                PageLanguageLabel.GestureRecognizers.Add(tap);
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
                        "Categories" => "Categories",
                        "Name" => "Name",
                        "New" => "New",
                        "Save" => "Save",
                        "Delete" => "Delete",
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

        private void UpdateLocalizedStrings()
        {
            PageHeaderText = Localize("Categories");
            NamePlaceholder = Localize("Name");
            NewButtonText = Localize("New");
            SaveButtonText = Localize("Save");
            DeleteButtonText = Localize("Delete");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                string code = _languageService?.CurrentCulture?.TwoLetterISOLanguageName ?? Preferences.Default.Get("biblio-culture", "nl");
                SetLanguageLabelFromCode(code);

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

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    SetLanguageLabelFromCode(culture.TwoLetterISOLanguageName);
                    UpdateLocalizedStrings();
                });
            }
            catch { }
        }

        private void SetLanguageLabelFromCode(string? code)
        {
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                var txt = code.ToLowerInvariant() == "en" ? "EN" : "NL";
                PageLanguageLabel.Text = txt;
                PageLanguageLabel.TextColor = Colors.White;
            }
            catch { }
        }

        private async void OnLanguageLabelTapped(object? sender, EventArgs e)
        {
            try
            {
                var action = await DisplayActionSheet("Taal", "Annuleren", null, "NL", "EN");
                if (string.IsNullOrEmpty(action) || action == "Annuleren") return;

                var code = action.ToLowerInvariant();
                try
                {
                    var svc = _languageService ?? App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                    svc?.SetLanguage(code);
                    SetLanguageLabelFromCode(code);
                    UpdateLocalizedStrings();
                }
                catch { }
            }
            catch { }
        }
    }
}
