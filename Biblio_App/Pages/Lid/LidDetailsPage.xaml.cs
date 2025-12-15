using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System;
using Biblio_App.ViewModels;
using Biblio_Models.Entiteiten;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using Microsoft.Extensions.DependencyInjection;
using Biblio_App.Services;
using System.Linq;

namespace Biblio_App.Pages
{
    public partial class LidDetailsPage : ContentPage, IQueryAttributable, ILocalizable
    {
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(LidDetailsPage), default(string));
        public static readonly BindableProperty EditButtonTextProperty = BindableProperty.Create(nameof(EditButtonText), typeof(string), typeof(LidDetailsPage), default(string));
        public static readonly BindableProperty BackButtonTextProperty = BindableProperty.Create(nameof(BackButtonText), typeof(string), typeof(LidDetailsPage), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string EditButtonText { get => (string)GetValue(EditButtonTextProperty); set => SetValue(EditButtonTextProperty, value); }
        public string BackButtonText { get => (string)GetValue(BackButtonTextProperty); set => SetValue(BackButtonTextProperty, value); }

        public LidDetailsPage()
        {
            InitializeComponent();
            try { _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }
            InitializeSharedResourceManager();
            UpdateLocalizedStrings();
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
                        "Members" => "Members",
                        "Details" => "Details",
                        "Edit" => "Edit",
                        "Back" => "Back",
                        _ => key
                    };
                }

                return key switch
                {
                    "Members" => "Leden",
                    "Details" => "Lid details",
                    "Edit" => "Bewerk",
                    "Back" => "Terug",
                    _ => key
                };
            }
            catch { return key; }
        }

        public void UpdateLocalizedStrings()
        {
            PageHeaderText = Localize("Details");
            EditButtonText = Localize("Edit");
            BackButtonText = Localize("Back");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
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

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
            }
            catch { }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query == null) return;
            var vm = this.BindingContext as LedenViewModel;
            if (query.TryGetValue("lidId", out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out var id))
                {
                    // probeer eerst te vinden in de reeds geladen collectie
                    var l = vm?.Leden.FirstOrDefault(x => x.Id == id);
                    if (l != null)
                    {
                        vm.SelectedLid = l;
                    }
                }
            }

            // als 'edit=true' is meegegeven, laat de velden bewerkbaar (SelectedLid wordt gezet).
            // Het viewmodel zal de velden vullen wanneer SelectedLid verandert.
        }

        private async void OnBackClicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
