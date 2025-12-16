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
using System.Reflection;

namespace Biblio_App.Pages
{
    public partial class LedenPagina : ContentPage, ILocalizable
    {
        private LedenViewModel VM => BindingContext as LedenViewModel;
        private ILanguageService? _language_service;
        private ResourceManager? _sharedResourceManager;

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty SearchPlaceholderTextProperty = BindableProperty.Create(nameof(SearchPlaceholderText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty SearchButtonTextProperty = BindableProperty.Create(nameof(SearchButtonText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty DetailsButtonTextProperty = BindableProperty.Create(nameof(DetailsButtonText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty EditButtonTextProperty = BindableProperty.Create(nameof(EditButtonText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty DeleteButtonTextProperty = BindableProperty.Create(nameof(DeleteButtonText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty SaveButtonTextProperty = BindableProperty.Create(nameof(SaveButtonText), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty PageFirstNamePlaceholderProperty = BindableProperty.Create(nameof(PageFirstNamePlaceholder), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty PageLastNamePlaceholderProperty = BindableProperty.Create(nameof(PageLastNamePlaceholder), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty PageEmailPlaceholderProperty = BindableProperty.Create(nameof(PageEmailPlaceholder), typeof(string), typeof(LedenPagina), default(string));
        public static readonly BindableProperty PagePhonePlaceholderProperty = BindableProperty.Create(nameof(PagePhonePlaceholder), typeof(string), typeof(LedenPagina), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string SearchPlaceholderText { get => (string)GetValue(SearchPlaceholderTextProperty); set => SetValue(SearchPlaceholderTextProperty, value); }
        public string SearchButtonText { get => (string)GetValue(SearchButtonTextProperty); set => SetValue(SearchButtonTextProperty, value); }
        public string DetailsButtonText { get => (string)GetValue(DetailsButtonTextProperty); set => SetValue(DetailsButtonTextProperty, value); }
        public string EditButtonText { get => (string)GetValue(EditButtonTextProperty); set => SetValue(EditButtonTextProperty, value); }
        public string DeleteButtonText { get => (string)GetValue(DeleteButtonTextProperty); set => SetValue(DeleteButtonTextProperty, value); }
        public string SaveButtonText { get => (string)GetValue(SaveButtonTextProperty); set => SetValue(SaveButtonTextProperty, value); }
        public string PageFirstNamePlaceholder { get => (string)GetValue(PageFirstNamePlaceholderProperty); set => SetValue(PageFirstNamePlaceholderProperty, value); }
        public string PageLastNamePlaceholder { get => (string)GetValue(PageLastNamePlaceholderProperty); set => SetValue(PageLastNamePlaceholderProperty, value); }
        public string PageEmailPlaceholder { get => (string)GetValue(PageEmailPlaceholderProperty); set => SetValue(PageEmailPlaceholderProperty, value); }
        public string PagePhonePlaceholder { get => (string)GetValue(PagePhonePlaceholderProperty); set => SetValue(PagePhonePlaceholderProperty, value); }

        public LedenPagina(LedenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            try
            {
                _language_service = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
            }
            catch { }

            InitializeSharedResourceManager();
            UpdateLocalizedStrings();

            try
            {
                if (vm is Biblio_App.Services.ILocalizable locVm)
                {
                }
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
                            var test = rm.GetString("Members", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
                                return;
                            }
                        }
                        catch { }
                    }
                }

                // fallback to model resource
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
                var culture = _language_service?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                if (_sharedResourceManager != null)
                {
                    var val = _sharedResourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(val)) return val;
                }

                // simple fallback for common keys
                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return key switch
                    {
                        "Members" => "Members",
                        "SearchPlaceholder" => "Search...",
                        "Search" => "Search",
              
                        "Details" => "Details",
                        "Edit" => "Edit",
                        "Delete" => "Delete",
                        "Save" => "Save",
                        "FirstName" => "First name",
                        "LastName" => "Last name",
                        "Email" => "Email",
                        "Phone" => "Phone",
                        _ => key
                    };
                }

                // default nl
                return key switch
                {
                    "Members" => "Leden",
                    "SearchPlaceholder" => "Zoeken...",
                    "Search" => "Zoek",
                 
                    "Details" => "Details",
                    "Edit" => "Bewerk",
                    "Delete" => "Verwijder",
                    "Save" => "Opslaan",
                    "FirstName" => "Voornaam",
                    "LastName" => "Achternaam",
                    "Email" => "Email",
                    "Phone" => "Telefoon",
                    _ => key
                };
            }
            catch { return key; }
        }

        public void UpdateLocalizedStrings()
        {
            PageHeaderText = Localize("Members");
            SearchPlaceholderText = Localize("SearchPlaceholder");
            SearchButtonText = Localize("Search");
            DetailsButtonText = Localize("Details");
            EditButtonText = Localize("Edit");
            DeleteButtonText = Localize("Delete");
            SaveButtonText = Localize("Save");
            PageFirstNamePlaceholder = Localize("FirstName");
            PageLastNamePlaceholder = Localize("LastName");
            PageEmailPlaceholder = Localize("Email");
            PagePhonePlaceholder = Localize("Phone");

            // explicitly refresh the title label in the title view
            try { RefreshTitleFromViewModel(); } catch { }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Ensure viewmodel has initialized data (loads members) when the page appears
                try
                {
                    if (VM != null)
                    {
                        await VM.InitializeAsync();
                    }
                }
                catch { }

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
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
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
                try { if (PageLanguageLabel != null) PageLanguageLabel.Text = title; } catch { }
            }
            catch { }
        }

        // New click handler for details ImageButton
        private async void OnDetailsClicked(object? sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Lid lid)
                {
                    // Set the selected item on the VM so the bound form fields are populated
                    try
                    {
                        if (VM != null)
                        {
                            VM.SelectedLid = lid;
                            return;
                        }
                    }
                    catch { }

                    // fallback: show details alert if VM not available
                    var title = Localize("Details");
                    var ok = Localize("OK");
                    var emailLabel = Localize("Email");
                    var phoneLabel = Localize("Phone");

                    var body = $"{lid.Voornaam} {lid.AchterNaam}\n{emailLabel}: {lid.Email}\n{phoneLabel}: {lid.Telefoon}";
                    await DisplayAlert(title, body, ok);
                }
            }
            catch { }
        }

        // New click handler for edit ImageButton
        private void OnEditClicked(object? sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Lid lid)
                {
                    try
                    {
                        if (VM != null)
                        {
                            VM.SelectedLid = lid;
                            // focus the first input so user can immediately edit
                            try { Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => VoornaamEntry?.Focus()); } catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
