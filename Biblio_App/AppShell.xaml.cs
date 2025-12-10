using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Biblio_App.Services;
using System;
using System.Resources;
using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace Biblio_App
{
    public partial class AppShell : Shell
    {
        public static AppShell? Instance { get; private set; }

        public ICommand LoginCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        private readonly ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;
        private Dictionary<string, string>? _resxFileStrings; // fallback strings loaded from resx files on disk

        public AppShell()
        {
            Instance = this;

            InitializeComponent();

            // Resolve language service from DI (may be null in design-time)
            try
            {
                _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += OnLanguageChangedExternally;
                }
            }
            catch { /* ignore */ }

            // Register routes so Shell navigation can use route strings
            Routing.RegisterRoute(nameof(Pages.BoekenPagina), typeof(Pages.BoekenPagina));
            Routing.RegisterRoute(nameof(Pages.LedenPagina), typeof(Pages.LedenPagina));
            Routing.RegisterRoute(nameof(Pages.UitleningenPagina), typeof(Pages.UitleningenPagina));
            Routing.RegisterRoute(nameof(Pages.CategorieenPagina), typeof(Pages.CategorieenPagina));
            Routing.RegisterRoute(nameof(Pages.Account.ProfilePage), typeof(Pages.Account.ProfilePage));
            Routing.RegisterRoute(nameof(Pages.MainPage), typeof(Pages.MainPage));
            Routing.RegisterRoute(nameof(Pages.InstellingenPagina), typeof(Pages.InstellingenPagina));
            Routing.RegisterRoute(nameof(Pages.BoekDetailsPage), typeof(Pages.BoekDetailsPage));
            Routing.RegisterRoute(nameof(Pages.LidDetailsPage), typeof(Pages.LidDetailsPage));

            // Use instance navigation methods to avoid depending on Shell.Current during construction
            LoginCommand = new Command(async () => await this.GoToAsync(nameof(Pages.Account.LoginPage)));
            OpenSettingsCommand = new Command(async () => await this.GoToAsync(nameof(Pages.InstellingenPagina)));

            // Initialize language picker
            try
            {
                // Always set the picker items so FR is always present
                LanguagePicker.ItemsSource = new List<string> { "NL", "EN", "FR" };

                // Prevent SelectedIndex change event from firing during initialization.
                try
                {
                    LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
                }
                catch { /* harmless if not yet attached */ }

                var cur = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                // Use ItemsSource where available, otherwise fallback to Items
                var itemsList = LanguagePicker.ItemsSource as IList<string> ?? LanguagePicker.Items.Cast<string>().ToList();
                if (cur == "en") LanguagePicker.SelectedIndex = Math.Max(0, itemsList.IndexOf("EN"));
                else if (cur == "fr") LanguagePicker.SelectedIndex = Math.Max(0, itemsList.IndexOf("FR"));
                else LanguagePicker.SelectedIndex = Math.Max(0, itemsList.IndexOf("NL"));

                // Re-attach after initial set so runtime changes still trigger handler.
                LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"AppShell initialization error: {ex}");
            }

            UpdateThemeIcon();

            // Initialize resource manager for localized flyout titles
            InitializeSharedResourceManager();
            UpdateLocalizedFlyoutTitles();

            // Runtime validation to catch missing pages / controls early
            try
            {
                ValidatePages();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ValidatePages error: {ex}");
            }
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                var webType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Biblio_Web.SharedResource", false))
                    .FirstOrDefault(t => t != null);
                if (webType != null)
                {
                    var asm = webType.Assembly;
                    _sharedResourceManager = new ResourceManager("Biblio_Web.Resources.Vertalingen.SharedResource", asm);
                    return;
                }
            }
            catch { }

            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (asm == null)
                {
                    try { asm = System.Reflection.Assembly.Load("Biblio_Web"); } catch { asm = null; }
                }

                if (asm != null)
                {
                    var candidates = new[] {
                        "Biblio_Web.Resources.Vertalingen.SharedResource",
                        "Biblio_Web.Resources.SharedResource",
                        "Biblio_Web.SharedResource"
                    };

                    foreach (var baseName in candidates)
                    {
                        try
                        {
                            var rm = new ResourceManager(baseName, asm);
                            var test = rm.GetString("Books", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
                                return;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                var modelType = typeof(Biblio_Models.Resources.SharedModelResource);
                if (modelType != null)
                {
                    var asm = modelType.Assembly;
                    _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", asm);
                    return;
                }
            }
            catch { }

            // As a developer convenience (local dev only), try to read the web project's resx files from disk
            TryLoadResxFromRepo();
        }

        private void TryLoadResxFromRepo()
        {
            try
            {
                // Walk up from the application's base directory to find the solution folder containing Biblio_Web
                var baseDir = AppContext.BaseDirectory;
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && dir != null; i++) // climb up a few levels
                {
                    var candidate = Path.Combine(dir.FullName, "Biblio_Web", "Resources", "Vertalingen");
                    if (Directory.Exists(candidate))
                    {
                        // Load the resx matching the current UI culture, fall back to two-letter code then 'nl'
                        var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                        var tryNames = new[] {
                            $"SharedResource.{culture.Name}.resx",
                            $"SharedResource.{culture.TwoLetterISOLanguageName}.resx",
                            "SharedResource.nl.resx"
                        };

                        foreach (var name in tryNames)
                        {
                            var path = Path.Combine(candidate, name);
                            if (File.Exists(path))
                            {
                                LoadResxFile(path);
                                return;
                            }
                        }

                        // also try non-vertalingen location
                        var candidate2 = Path.Combine(dir.FullName, "Biblio_Web", "Resources");
                        foreach (var name in tryNames)
                        {
                            var path = Path.Combine(candidate2, name);
                            if (File.Exists(path))
                            {
                                LoadResxFile(path);
                                return;
                            }
                        }
                    }

                    dir = dir.Parent;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryLoadResxFromRepo error: {ex}");
            }
        }

        private void LoadResxFile(string path)
        {
            try
            {
                var doc = XDocument.Load(path);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var data in doc.Root.Elements("data"))
                {
                    var name = data.Attribute("name")?.Value;
                    var val = data.Element("value")?.Value;
                    if (!string.IsNullOrEmpty(name) && val != null)
                    {
                        dict[name] = val;
                    }
                }
                _resxFileStrings = dict;
                Debug.WriteLine($"Loaded resx fallback from {path} with {_resxFileStrings.Count} keys.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadResxFile error: {ex}");
            }
        }

        private string Localize(string resourceKey)
        {
            try
            {
                var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;

                // 1) try ResourceManager if available
                if (_sharedResourceManager != null)
                {
                    try
                    {
                        var val = _sharedResourceManager.GetString(resourceKey, culture);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    catch { }
                }

                // 2) try resx file loaded from repo (developer machine)
                if (_resxFileStrings != null)
                {
                    if (_resxFileStrings.TryGetValue(resourceKey, out var v) && !string.IsNullOrEmpty(v))
                    {
                        return v;
                    }
                }

                // 3) fallback hard-coded per-language defaults
                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return resourceKey switch
                    {
                        "Books" => "Books",
                        "Members" => "Members",
                        "Loans" => "Loans",
                        "Categories" => "Categories",
                        "Settings" => "Settings",
                        "Profile" => "Profile",
                        "Logout" => "Logout",
                        "QuickActions" => "Quick actions",
                        "NewBook" => "New book",
                        "NewMember" => "New member",
                        _ => resourceKey
                    };
                }

                if (code == "fr")
                {
                    return resourceKey switch
                    {
                        "Books" => "Livres",
                        "Members" => "Membres",
                        "Loans" => "Prêts",
                        "Categories" => "Catégories",
                        "Settings" => "Paramètres",
                        "Profile" => "Profil",
                        "Logout" => "Se déconnecter",
                        "QuickActions" => "Raccourcis",
                        "NewBook" => "Nouveau livre",
                        "NewMember" => "Nouveau membre",
                        _ => resourceKey
                    };
                }

                // Dutch / default
                return resourceKey switch
                {
                    "Books" => "Boeken",
                    "Members" => "Leden",
                    "Loans" => "Uitleningen",
                    "Categories" => "Categorieën",
                    "Settings" => "Instellingen",
                    "Profile" => "Profiel",
                    "Logout" => "Logout",
                    "QuickActions" => "Snelkoppelingen",
                    "NewBook" => "Nieuw boek",
                    "NewMember" => "Nieuw lid",
                    _ => resourceKey
                };
            }
            catch { return resourceKey; }
        }

        private void UpdateLocalizedFlyoutTitles()
        {
            try
            {
                if (BooksShell != null) BooksShell.Title = $"📚 {Localize("Books")}";
                if (MembersShell != null) MembersShell.Title = $"👥 {Localize("Members")}";
                if (LoansShell != null) LoansShell.Title = $"🧾 {Localize("Loans")}";
                if (CategoriesShell != null) CategoriesShell.Title = $"🏷️ {Localize("Categories")}";
                if (SettingsShell != null) SettingsShell.Title = $"⚙️ {Localize("Settings")}";

                // SettingsMenuItem was removed from XAML; update remaining items
                if (ProfileMenuItem != null) ProfileMenuItem.Text = $"👤 {Localize("Profile")}";
                if (LogoutMenuItem != null) LogoutMenuItem.Text = $"🔒 {Localize("Logout")}";

                if (QuickLabel != null) QuickLabel.Text = Localize("QuickActions");
                if (QuickNewBookButton != null) QuickNewBookButton.Text = $"+ {Localize("NewBook")}";
                if (QuickNewMemberButton != null) QuickNewMemberButton.Text = $"+ {Localize("NewMember")}";
                if (FooterSettingsButton != null) FooterSettingsButton.Text = Localize("Settings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateLocalizedFlyoutTitles error: {ex}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged -= OnLanguageChangedExternally;
                }
            }
            catch { }
        }

        private void UpdateThemeIcon()
        {
            try
            {
                var isDark = Application.Current?.UserAppTheme == AppTheme.Dark || Application.Current?.RequestedTheme == AppTheme.Dark;
                var img = new Image { Source = isDark ? "moon.svg" : "sun.svg", WidthRequest = 18, HeightRequest = 18 };
                // Use Image as icon inside the Button by setting ImageSource
                ThemeToggleButton.ImageSource = img.Source;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"UpdateThemeIcon error: {ex}");
            }
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            try
            {
                var current = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
                var next = (current == AppTheme.Dark) ? AppTheme.Light : AppTheme.Dark;
                Application.Current.UserAppTheme = next;
                try { Preferences.Default.Set("biblio-theme", next == AppTheme.Dark ? "dark" : "light"); } catch { }
                UpdateThemeIcon();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnThemeToggleClicked error: {ex}");
            }
        }

        // Called when the picker selection changes - delegate to the language service
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            try
            {
                if (LanguagePicker.SelectedIndex < 0) return;
                // Normalize selection value to two-letter lowercase code
                var raw = LanguagePicker.Items[LanguagePicker.SelectedIndex];
                var code = (raw ?? string.Empty).Trim().ToLowerInvariant();

                // Support values like "nl", "en", "fr" or uppercase "NL" etc.
                if (code.Length > 2) code = code.Substring(0, 2);

                if (_languageService != null)
                {
                    _languageService.SetLanguage(code);
                }
                else
                {
                    // fallback behavior if service not available
                    var culture = new CultureInfo(code);
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    try { Preferences.Default.Set("biblio-culture", code); } catch { }
                    // simple UI refresh
                    try { Application.Current.MainPage = new AppShell(); } catch { }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnLanguageChanged error: {ex}");
            }
        }

        // React to language changes triggered elsewhere (or via the service)
        private void OnLanguageChangedExternally(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await DisplayAlert("Taal", $"Taal ingesteld op: {culture.TwoLetterISOLanguageName}. De interface wordt nu verfrist.", "OK");
                    }
                    catch { }

                    try
                    {
                        // recreate main page (new shell) so pages, resources and controls are rebuilt using the new culture
                        Application.Current.MainPage = new AppShell();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"Failed to refresh UI after culture change: {ex}");
                    }
                });
            }
            catch { }
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            try
            {
                var email = Preferences.Default.ContainsKey("CurrentEmail") ? Preferences.Default.Get("CurrentEmail", string.Empty) : null;
                if (string.IsNullOrEmpty(email))
                {
                    await DisplayAlert("Profiel", "Geen ingelogde gebruiker. Log in via de web-app of Identity UI.", "OK");
                    return;
                }

                await this.GoToAsync(nameof(Pages.Account.ProfilePage));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnProfileClicked error: {ex}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                Preferences.Default.Remove("CurrentEmail");
                Preferences.Default.Remove("IsAdmin");
                Preferences.Default.Remove("IsStaff");

                var sec = App.Current?.Handler?.MauiContext?.Services?.GetService<ViewModels.SecurityViewModel>();
                sec?.Reset();

                await DisplayAlert("Logout", "Je bent afgemeld.", "OK");
                await this.GoToAsync("//");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnLogoutClicked error: {ex}");
            }
        }

        private async void QuickNewBook_Clicked(object sender, EventArgs e)
        {
            try
            {
                await this.GoToAsync(nameof(Pages.BoekenPagina));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"QuickNewBook_Clicked error: {ex}");
            }
        }

        private async void QuickNewMember_Clicked(object sender, EventArgs e)
        {
            try
            {
                await this.GoToAsync(nameof(Pages.LedenPagina));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"QuickNewMember_Clicked error: {ex}");
            }
        }

        // Runtime validation helper to surface missing pages / null controls early.
        private void ValidatePages()
        {
            // Validate that registered page types actually exist
            var registered = new (string Route, System.Type Type)[]
            {
                (nameof(Pages.BoekenPagina), typeof(Pages.BoekenPagina)),
                (nameof(Pages.LedenPagina), typeof(Pages.LedenPagina)),
                (nameof(Pages.UitleningenPagina), typeof(Pages.UitleningenPagina)),
                (nameof(Pages.CategorieenPagina), typeof(Pages.CategorieenPagina)),
                (nameof(Pages.Account.ProfilePage), typeof(Pages.Account.ProfilePage)),
                (nameof(Pages.MainPage), typeof(Pages.MainPage)),
                (nameof(Pages.Account.LoginPage), typeof(Pages.Account.LoginPage)),
                (nameof(Pages.InstellingenPagina), typeof(Pages.InstellingenPagina)),
                (nameof(Pages.BoekDetailsPage), typeof(Pages.BoekDetailsPage)),
                (nameof(Pages.LidDetailsPage), typeof(Pages.LidDetailsPage))
            };

            foreach (var r in registered)
            {
                if (r.Type == null)
                {
                    Debug.WriteLine($"Route registration: type for route '{r.Route}' is null.");
                }
            }

            // Validate important controls were created by XAML
            var controls = new (string Name, object? Control)[]
            {
                ("LanguagePicker", LanguagePicker),
                ("QuickNewBookButton", QuickNewBookButton),
                ("QuickNewMemberButton", QuickNewMemberButton),
                ("ProfileMenuItem", ProfileMenuItem),
                ("LogoutMenuItem", LogoutMenuItem),
                ("ThemeToggleButton", ThemeToggleButton)
            };

            foreach (var c in controls)
            {
                if (c.Control is null)
                {
                    Debug.WriteLine($"Missing control: {c.Name} is null. Check AppShell.xaml for correct x:Name.");
                }
            }

            // Basic sanity for commands
            if (LoginCommand == null) Debug.WriteLine("LoginCommand is null.");
            if (OpenSettingsCommand == null) Debug.WriteLine("OpenSettingsCommand is null.");
        }
    }
}
