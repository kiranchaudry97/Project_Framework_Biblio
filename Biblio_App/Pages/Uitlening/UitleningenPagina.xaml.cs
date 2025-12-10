using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using Microsoft.Maui.Graphics;
using Biblio_App.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using System.Linq;

namespace Biblio_App.Pages
{
    public partial class UitleningenPagina : ContentPage
    {
        private UitleningenViewModel VM => BindingContext as UitleningenViewModel;
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty MembersLabelProperty = BindableProperty.Create(nameof(MembersLabel), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty LoansLabelProperty = BindableProperty.Create(nameof(LoansLabel), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty BooksLabelProperty = BindableProperty.Create(nameof(BooksLabel), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty DbPathLabelTextProperty = BindableProperty.Create(nameof(DbPathLabelText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty CopyPathButtonTextProperty = BindableProperty.Create(nameof(CopyPathButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty SearchPlaceholderTextProperty = BindableProperty.Create(nameof(SearchPlaceholderText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty FilterButtonTextProperty = BindableProperty.Create(nameof(FilterButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty CategoryTitleProperty = BindableProperty.Create(nameof(CategoryTitle), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty MemberTitleProperty = BindableProperty.Create(nameof(MemberTitle), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty BookTitleProperty = BindableProperty.Create(nameof(BookTitle), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty OnlyOpenTextProperty = BindableProperty.Create(nameof(OnlyOpenText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty ViewButtonTextProperty = BindableProperty.Create(nameof(ViewButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty ReturnButtonTextProperty = BindableProperty.Create(nameof(ReturnButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty NewButtonTextProperty = BindableProperty.Create(nameof(NewButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty SaveButtonTextProperty = BindableProperty.Create(nameof(SaveButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty DeleteButtonTextProperty = BindableProperty.Create(nameof(DeleteButtonText), typeof(string), typeof(UitleningenPagina), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string MembersLabel { get => (string)GetValue(MembersLabelProperty); set => SetValue(MembersLabelProperty, value); }
        public string LoansLabel { get => (string)GetValue(LoansLabelProperty); set => SetValue(LoansLabelProperty, value); }
        public string BooksLabel { get => (string)GetValue(BooksLabelProperty); set => SetValue(BooksLabelProperty, value); }
        public string DbPathLabelText { get => (string)GetValue(DbPathLabelTextProperty); set => SetValue(DbPathLabelTextProperty, value); }
        public string CopyPathButtonText { get => (string)GetValue(CopyPathButtonTextProperty); set => SetValue(CopyPathButtonTextProperty, value); }
        public string SearchPlaceholderText { get => (string)GetValue(SearchPlaceholderTextProperty); set => SetValue(SearchPlaceholderTextProperty, value); }
        public string FilterButtonText { get => (string)GetValue(FilterButtonTextProperty); set => SetValue(FilterButtonTextProperty, value); }
        public string CategoryTitle { get => (string)GetValue(CategoryTitleProperty); set => SetValue(CategoryTitleProperty, value); }
        public string MemberTitle { get => (string)GetValue(MemberTitleProperty); set => SetValue(MemberTitleProperty, value); }
        public string BookTitle { get => (string)GetValue(BookTitleProperty); set => SetValue(BookTitleProperty, value); }
        public string OnlyOpenText { get => (string)GetValue(OnlyOpenTextProperty); set => SetValue(OnlyOpenTextProperty, value); }
        public string ViewButtonText { get => (string)GetValue(ViewButtonTextProperty); set => SetValue(ViewButtonTextProperty, value); }
        public string ReturnButtonText { get => (string)GetValue(ReturnButtonTextProperty); set => SetValue(ReturnButtonTextProperty, value); }
        public string NewButtonText { get => (string)GetValue(NewButtonTextProperty); set => SetValue(NewButtonTextProperty, value); }
        public string SaveButtonText { get => (string)GetValue(SaveButtonTextProperty); set => SetValue(SaveButtonTextProperty, value); }
        public string DeleteButtonText { get => (string)GetValue(DeleteButtonTextProperty); set => SetValue(DeleteButtonTextProperty, value); }

        public UitleningenPagina(UitleningenViewModel vm)
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
                        "Loans" => "Loans",
                        "Books" => "Books",
                        "DbPath" => "DB path:",
                        "CopyPath" => "Copy path",
                        "SearchPlaceholder" => "Search...",
                        "Filter" => "Filter",
                        "Category" => "Category",
                        "Member" => "Member",
                        "Book" => "Book",
                        "OnlyOpen" => "Only open",
                        "View" => "View",
                        "Return" => "Return",
                        "New" => "New",
                        "Save" => "Save",
                        "Delete" => "Delete",
                        _ => key
                    };
                }

                return key switch
                {
                    "Members" => "Leden",
                    "Loans" => "Uitleningen",
                    "Books" => "Boeken",
                    "DbPath" => "DB pad:",
                    "CopyPath" => "Kopieer pad",
                    "SearchPlaceholder" => "Zoeken...",
                    "Filter" => "Filter",
                    "Category" => "Categorie",
                    "Member" => "Lid",
                    "Book" => "Boek",
                    "OnlyOpen" => "Alleen open",
                    "View" => "Inzien",
                    "Return" => "Inleveren",
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
            PageHeaderText = Localize("Loans");
            MembersLabel = Localize("Members");
            LoansLabel = Localize("Loans");
            BooksLabel = Localize("Books");
            DbPathLabelText = Localize("DbPath");
            CopyPathButtonText = Localize("CopyPath");
            SearchPlaceholderText = Localize("SearchPlaceholder");
            FilterButtonText = Localize("Filter");
            CategoryTitle = Localize("Category");
            MemberTitle = Localize("Member");
            BookTitle = Localize("Book");
            OnlyOpenText = Localize("OnlyOpen");
            ViewButtonText = Localize("View");
            ReturnButtonText = Localize("Return");
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

        private async void OnCopyDbPathClicked(object sender, EventArgs e)
        {
            try
            {
                var lbl = this.FindByName<Label>("DbPathLabel");
                var text = lbl?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    await Clipboard.SetTextAsync(text);
                    await DisplayAlert("Gekopieerd", "Database-pad is gekopieerd naar klembord.", "OK");
                }
                else
                {
                    await DisplayAlert("Leeg", "Er is geen pad geladen.", "OK");
                }
            }
            catch { }
        }
    }
}
