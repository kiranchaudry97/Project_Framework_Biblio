using Microsoft.Maui.Controls;
using Biblio_Models.Entiteiten;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using Biblio_App.Services;
using Microsoft.Maui.Graphics;

namespace Biblio_App.Pages
{
    public partial class BoekDetailsPage : ContentPage, IQueryAttributable, ILocalizable
    {
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(BoekDetailsPage), default(string));
        public static readonly BindableProperty EditButtonTextProperty = BindableProperty.Create(nameof(EditButtonText), typeof(string), typeof(BoekDetailsPage), default(string));
        public static readonly BindableProperty BackButtonTextProperty = BindableProperty.Create(nameof(BackButtonText), typeof(string), typeof(BoekDetailsPage), default(string));
        public static readonly BindableProperty CategoryLabelProperty = BindableProperty.Create(nameof(CategoryLabel), typeof(string), typeof(BoekDetailsPage), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string EditButtonText { get => (string)GetValue(EditButtonTextProperty); set => SetValue(EditButtonTextProperty, value); }
        public string BackButtonText { get => (string)GetValue(BackButtonTextProperty); set => SetValue(BackButtonTextProperty, value); }
        public string CategoryLabel { get => (string)GetValue(CategoryLabelProperty); set => SetValue(CategoryLabelProperty, value); }

        public BoekDetailsPage()
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
                        "Details" => "Details",
                        "Edit" => "Edit",
                        "Back" => "Back",
                        "Category" => "Category",
                        _ => key
                    };
                }

                return key switch
                {
                    "Details" => "Boek details",
                    "Edit" => "Bewerk",
                    "Back" => "Terug",
                    "Category" => "Categorie",
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
            CategoryLabel = Localize("Category");
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
            if (query.TryGetValue("boekId", out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out var id))
                {
                    _ = LoadBoekAsync(id);
                }
            }
        }

        private async Task LoadBoekAsync(int id)
        {
            try
            {
                var svc = App.Current?.Handler?.MauiContext?.Services;
                var factory = svc?.GetService<IDbContextFactory<BiblioDbContext>>();
                if (factory != null)
                {
                    using var db = factory.CreateDbContext();
                    var boek = await db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
                    if (boek != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => BindingContext = boek);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("..", true);
            }
            catch { }
        }
    }
}
