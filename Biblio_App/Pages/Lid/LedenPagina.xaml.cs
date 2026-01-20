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
        // Short-hand naar de gekoppelde ViewModel
        private LedenViewModel VM => BindingContext as LedenViewModel;

        // Service + resources voor taal/vertaling
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

            // MVVM: ViewModel koppelen aan de pagina
            BindingContext = vm;

            try
            {
                // Shell navigatie/Back-knop gedrag instellen
                try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                try { Shell.SetFlyoutBehavior(this, FlyoutBehavior.Flyout); } catch { }
                try { NavigationPage.SetHasBackButton(this, false); } catch { }
            }
            catch { }

            try
            {
                // LanguageService uit DI halen (kan null zijn in design-time)
                _language_service = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
            }
            catch { }

            InitializeSharedResourceManager();
            // Pagina-teksten initialiseren (header, placeholders, knoppen)
            UpdateLocalizedStrings();

            // Initialize ViewModel's localized strings
            try
            {
                if (vm is Biblio_App.Services.ILocalizable locVm)
                {
                    // ViewModel heeft ook gelokaliseerde strings (labels), dus die updaten we ook
                    locVm.UpdateLocalizedStrings();
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

                // fallback naar model resource
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

                // eenvoudige fallback voor veelgebruikte sleutels
                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return key switch
                    {
                        "Members" => "Members",
                        "SearchPlaceholder" => "Search...",
                        "Search" => "Search",
              
                        "Overview" => "Overview",
                        "Details" => "Details",
                        "Edit" => "Edit",
                        "Delete" => "Delete",
                        "Save" => "Save",
                        "New" => "New",
                        "Cancel" => "Cancel",
                        "OK" => "OK",
                        "FirstName" => "First name",
                        "LastName" => "Last name",
                        "Email" => "Email",
                        "Phone" => "Phone",
                        _ => key
                    };
                }

                // standaard nl
                return key switch
                {
                    "Members" => "Leden",
                    "SearchPlaceholder" => "Zoeken...",
                    "Search" => "Zoek",
                 
                    "Overview" => "Overzicht",
                    "Details" => "Details",
                    "Edit" => "Bewerk",
                    "Delete" => "Verwijder",
                    "Save" => "Opslaan",
                    "New" => "Nieuw",
                    "Cancel" => "Annuleren",
                    "OK" => "OK",
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

            // vernieuw expliciet het titel label in de titel weergave
            try { RefreshTitleFromViewModel(); } catch { }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Zorg ervoor dat viewmodel data heeft geïnitialiseerd (laadt leden) wanneer de pagina verschijnt
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
                    // Luister naar taalwijzigingen zodat de UI live update
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
                    // Unsubscribe om memory leaks te vermijden
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

        // Click handler for details Button
        private async void OnDetailsClicked(object? sender, EventArgs e)
        {
            // Details/bewerk/delete zitten in de lijst. Via BindingContext weten we welk item is aangeklikt.
            try
            {
                System.Diagnostics.Debug.WriteLine("=== OnDetailsClicked TRIGGERED ===");
                
                // Get the Lid from the Button's BindingContext
                // Visual tree: Frame ? Grid ? HorizontalStackLayout ? Button
                // So we need to go 3 levels up: Button.Parent.Parent.Parent = Frame
                var button = sender as Button;
                System.Diagnostics.Debug.WriteLine($"Button: {button != null}");
                
                var horizontalStack = button?.Parent; // HorizontalStackLayout
                System.Diagnostics.Debug.WriteLine($"HorizontalStackLayout: {horizontalStack?.GetType().Name}");
                
                var grid = horizontalStack?.Parent; // Grid
                System.Diagnostics.Debug.WriteLine($"Grid: {grid?.GetType().Name}");
                
                var frame = grid?.Parent as Frame; // Frame
                System.Diagnostics.Debug.WriteLine($"Frame: {frame != null}");
                
                var lid = frame?.BindingContext as Biblio_Models.Entiteiten.Lid;
                System.Diagnostics.Debug.WriteLine($"Lid: {lid?.Voornaam} {lid?.AchterNaam}");
                
                if (lid != null)
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

        // Click handler for edit Button
        private void OnEditClicked(object? sender, EventArgs e)
        {
            try
            {
                // Visual tree: Frame ? Grid ? HorizontalStackLayout ? Button
                var button = sender as Button;
                var frame = button?.Parent?.Parent?.Parent as Frame;
                var lid = frame?.BindingContext as Biblio_Models.Entiteiten.Lid;
                
                if (lid != null)
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

        // Click handler for delete Button
        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            try
            {
                // Visual tree: Frame ? Grid ? HorizontalStackLayout ? Button
                var button = sender as Button;
                var frame = button?.Parent?.Parent?.Parent as Frame;
                var lid = frame?.BindingContext as Biblio_Models.Entiteiten.Lid;
                
                if (lid != null)
                {
                    var deleteText = Localize("Delete");
                    var cancelText = Localize("Cancel");
                    var confirmMessage = $"{Localize("Delete")} {lid.Voornaam} {lid.AchterNaam}?";
                    
                    bool confirm = await DisplayAlert(deleteText, confirmMessage, deleteText, cancelText);
                    
                    if (confirm && VM != null)
                    {
                        await VM.ItemDeleteCommand.ExecuteAsync(lid);
                    }
                }
            }
            catch { }
        }
    }
}
