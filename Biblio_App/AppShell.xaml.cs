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

        // Niet readonly maken zodat we het later kunnen resolven wanneer MauiContext beschikbaar is
        private ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;
        private Dictionary<string, string>? _resxFileStrings; // fallback strings geladen uit resx-bestanden op schijf

        // Bindable properties gebruikt door XAML zodat teksten op runtime bijgewerkt kunnen worden zonder controls te recreëren
        public string BiblioTitle { get; private set; } = "Biblio";
        public string MenuTitle { get; private set; } = "Menu";
        public string LanguagePickerTitle { get; private set; } = "Taal";
        public string BooksShellTitle { get; private set; } = "📚 Boeken";
        public string MembersShellTitle { get; private set; } = "👥 Leden";
        public string LoansShellTitle { get; private set; } = "🧾 Uitleningen";
        public string CategoriesShellTitle { get; private set; } = "🏷️ Categorieën";
        public string SettingsShellTitle { get; private set; } = "⚙️ Instellingen";
        public string ProfileMenuText { get; private set; } = "👤 Profiel";
        public string LogoutMenuText { get; private set; } = "🔒 Logout";
        public string QuickLabelText { get; private set; } = "Snelkoppelingen";
        public string QuickNewBookButtonText { get; private set; } = "+ Nieuw boek";
        public string QuickNewMemberButtonText { get; private set; } = "+ Nieuw lid";
        public string FooterSettingsText { get; private set; } = "Instellingen";

        public AppShell()
        {
            Instance = this;

            InitializeComponent();

            // Resolveer language service uit DI (kan null zijn in design-time)
            try
            {
                _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += OnLanguageChangedExternally;
                }
            }
            catch { /* negeren */ }

            // Registreer routes zodat Shell navigatie route strings kan gebruiken
            Routing.RegisterRoute(nameof(Pages.BoekenPagina), typeof(Pages.BoekenPagina));
            Routing.RegisterRoute(nameof(Pages.LedenPagina), typeof(Pages.LedenPagina));
            Routing.RegisterRoute(nameof(Pages.UitleningenPagina), typeof(Pages.UitleningenPagina));
            Routing.RegisterRoute(nameof(Pages.CategorieenPagina), typeof(Pages.CategorieenPagina));
            Routing.RegisterRoute(nameof(Pages.Account.ProfilePage), typeof(Pages.Account.ProfilePage));
            Routing.RegisterRoute(nameof(Pages.MainPage), typeof(Pages.MainPage));
            Routing.RegisterRoute(nameof(Pages.InstellingenPagina), typeof(Pages.InstellingenPagina));
            Routing.RegisterRoute(nameof(Pages.BoekDetailsPage), typeof(Pages.BoekDetailsPage));
            Routing.RegisterRoute(nameof(Pages.LidDetailsPage), typeof(Pages.LidDetailsPage));

            // Gebruik instance-navigatiemethoden om niet te vertrouwen op Shell.Current tijdens constructie
            LoginCommand = new Command(async () => await this.GoToAsync(nameof(Pages.Account.LoginPage)));
            OpenSettingsCommand = new Command(async () => await this.GoToAsync(nameof(Pages.InstellingenPagina)));

            UpdateThemeIcon();

            // Initialiseer resource manager voor gelokaliseerde flyout-titels
            InitializeSharedResourceManager();
            UpdateLocalizedFlyoutTitles();

            // Runtime validatie om ontbrekende pages/controls vroegtijdig te detecteren
            try
            {
                ValidatePages();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ValidatePages error: {ex}");
            }

            // Zorg dat zichtbare pagina's hun lokalisatie verversen wanneer navigatie plaatsvindt
            try
            {
                this.Navigated += OnShellNavigated;
            }
            catch { }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Op sommige platforms is MauiContext/DI mogelijk niet beschikbaar in de constructor.
            // Los de language service nu op als die nog null is en abonneer op events.
            try
            {
                if (_languageService == null)
                {
                    var svc = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                    if (svc != null)
                    {
                        _languageService = svc;
                        _languageService.LanguageChanged += OnLanguageChangedExternally;

                        // Zorg ervoor dat het menu onmiddellijk de huidige cultuur weerspiegelt
                        try { UpdateLocalizedFlyoutTitles(); } catch { }
                    }
                }
            }
            catch { }
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            try
            {
                var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                // Ververs de nieuw zichtbare pagina en diens viewmodel om gelokaliseerde strings toe te passen
                try { RefreshVisiblePagesLocalizations(culture); } catch { }
            }
            catch { }
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                // Geef voorkeur aan de statische ResourceManager van de gedeelde modelresource zodat alle projecten dezelfde manager gebruiken
                _sharedResourceManager = Biblio_Models.Resources.SharedModelResource.ResourceManager;
                return;
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

            // Als ontwikkelaarsgemak (alleen lokale dev), probeer resx-bestanden van het web-project vanaf schijf te lezen
            TryLoadResxFromRepo();
        }

        private void TryLoadResxFromRepo()
        {
            try
            {
                // Loop omhoog vanaf de applicatie base directory om de solution folder met Biblio_Web te vinden
                var baseDir = AppContext.BaseDirectory;
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && dir != null; i++) // klim een paar niveaus omhoog
                {
                    var candidate = Path.Combine(dir.FullName, "Biblio_Web", "Resources", "Vertalingen");
                    if (Directory.Exists(candidate))
                    {
                        // Laad de resx die overeenkomt met de huidige UI cultuur, val terug op twee-letter code en daarna 'nl'
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

                        // probeer ook een alternatieve locatie zonder 'Vertalingen'
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

                // 1) probeer ResourceManager indien beschikbaar
                if (_sharedResourceManager != null)
                {
                    try
                    {
                        var val = _sharedResourceManager.GetString(resourceKey, culture);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    catch { }
                }

                // 2) probeer resx-bestand geladen van repo (ontwikkelaarsmachine)
                if (_resxFileStrings != null)
                {
                    if (_resxFileStrings.TryGetValue(resourceKey, out var v) && !string.IsNullOrEmpty(v))
                    {
                        return v;
                    }
                }

                // 3) fallback hard-coded per-taal defaults
                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return resourceKey switch
                    {
                        "SearchPlaceholder" => "Search...",
                        "Search" => "Search",
                        "Details" => "Details",
                        "Edit" => "Edit",
                        "Delete" => "Delete",
                        "New" => "New",
                        "Save" => "Save",
                        "View" => "View",
                        "Return" => "Return",
                        "CopyPath" => "Copy path",
                        "DbPath" => "DB path:",
                        "All" => "All",
                        "Category" => "Category",
                        "Member" => "Member",
                        "Book" => "Book",
                        "OnlyOpen" => "Only open",
                        "Filter" => "Filter",
                        "FirstName" => "First name",
                        "LastName" => "Last name",
                        "Email" => "Email",
                        "Phone" => "Phone",
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
                        "LanguageChangedTitle" => "Language changed",
                        "LanguageChangedBody" => "Language set to: {0}. The interface has been updated.",
                        "LanguageSetTo" => "Language set to: {0}",
                        "OK" => "OK",
                        _ => resourceKey
                    };
                }

                if (code == "fr")
                {
                    return resourceKey switch
                    {
                        "SearchPlaceholder" => "Rechercher...",
                        "Search" => "Rechercher",
                        "Details" => "Détails",
                        "Edit" => "Modifier",
                        "Delete" => "Supprimer",
                        "New" => "Nouveau",
                        "Save" => "Enregistrer",
                        "View" => "Voir",
                        "Return" => "Retourner",
                        "CopyPath" => "Copier le chemin",
                        "DbPath" => "Chemin DB:",
                        "All" => "Tous",
                        "Category" => "Catégorie",
                        "Member" => "Membre",
                        "Book" => "Livre",
                        "OnlyOpen" => "Seulement ouverts",
                        "Filter" => "Filtrer",
                        "FirstName" => "Prénom",
                        "LastName" => "Nom",
                        "Email" => "Email",
                        "Phone" => "Téléphone",
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
                        "LanguageChangedTitle" => "Langue modifiée",
                        "LanguageChangedBody" => "Langue définie sur : {0}. L’interface a été mise à jour.",
                        "OK" => "OK",
                        _ => resourceKey
                    };
                }

                // Nederlands / default
                return resourceKey switch
                {
                    "SearchPlaceholder" => "Zoeken...",
                    "Search" => "Zoek",
                    "Details" => "Details",
                    "Edit" => "Bewerk",
                    "Delete" => "Verwijder",
                    "New" => "Nieuw",
                    "Save" => "Opslaan",
                    "View" => "Inzien",
                    "Return" => "Inleveren",
                    "CopyPath" => "Kopieer pad",
                    "DbPath" => "DB pad:",
                    "All" => "Alle",
                    "Category" => "Categorie",
                    "Member" => "Lid",
                    "Book" => "Boek",
                    "OnlyOpen" => "Alleen open",
                    "Filter" => "Filter",
                    "FirstName" => "Voornaam",
                    "LastName" => "Achternaam",
                    "Email" => "Email",
                    "Phone" => "Telefoon",
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
                    "LanguageChangedTitle" => "Taal gewijzigd",
                    "LanguageChangedBody" => "Taal ingesteld op: {0}. De interface is bijgewerkt.",
                    "LanguageSetTo" => "Taal gedefinieerd op {0}",
                    "OK" => "OK",
                    _ => resourceKey
                };
            }
            catch { return resourceKey; }
        }

        // Publieke wrapper zodat andere componenten (viewmodels/pages) lokalisatie kunnen aanroepen zonder reflectie.
        public string Translate(string resourceKey)
        {
            try
            {
                return Localize(resourceKey) ?? resourceKey;
            }
            catch { return resourceKey; }
        }

        private void UpdateLocalizedFlyoutTitles()
        {
            try
            {
                // Werk bindable properties bij in plaats van UI-controls direct te wijzigen.
                BooksShellTitle = $"📚 {Localize("Books")}";
                MembersShellTitle = $"👥 {Localize("Members")}";
                LoansShellTitle = $"🧾 {Localize("Loans")}";
                CategoriesShellTitle = $"🏷️ {Localize("Categories")}";
                SettingsShellTitle = $"⚙️ {Localize("Settings")}";
                MenuTitle = Localize("Menu");
                
                ProfileMenuText = $"👤 {Localize("Profile")}";
                LogoutMenuText = $"🔒 {Localize("Logout")}";
                
                QuickLabelText = Localize("QuickActions");
                QuickNewBookButtonText = $"+ {Localize("NewBook")}";
                QuickNewMemberButtonText = $"+ {Localize("NewMember")}";
                FooterSettingsText = Localize("Settings");
                
                // Houd de app-titel constant (niet lokaliseren)
                BiblioTitle = "Biblio";
                LanguagePickerTitle = Localize("LanguagePickerTitle") ?? "Taal";

                // Notify bindings dat properties veranderd zijn
                OnPropertyChanged(nameof(BooksShellTitle));
                OnPropertyChanged(nameof(MembersShellTitle));
                OnPropertyChanged(nameof(LoansShellTitle));
                OnPropertyChanged(nameof(CategoriesShellTitle));
                OnPropertyChanged(nameof(SettingsShellTitle));
                OnPropertyChanged(nameof(ProfileMenuText));
                OnPropertyChanged(nameof(LogoutMenuText));
                OnPropertyChanged(nameof(QuickLabelText));
                OnPropertyChanged(nameof(QuickNewBookButtonText));
                OnPropertyChanged(nameof(QuickNewMemberButtonText));
                OnPropertyChanged(nameof(FooterSettingsText));
                OnPropertyChanged(nameof(BiblioTitle));
                OnPropertyChanged(nameof(LanguagePickerTitle));
                OnPropertyChanged(nameof(MenuTitle));

                // Update ook daadwerkelijk UI-elementen Titles/Text direct omdat Shell sommige bindings mogelijk niet onmiddellijk ververst
                try
                {
                    // Update ShellContent titels indien benoemd in XAML
                    try { BooksShell.Title = BooksShellTitle; } catch { }
                    try { MembersShell.Title = MembersShellTitle; } catch { }
                    try { LoansShell.Title = LoansShellTitle; } catch { }
                    try { CategoriesShell.Title = CategoriesShellTitle; } catch { }
                    try { SettingsShell.Title = SettingsShellTitle; } catch { }

                    // Update menu/flyout teksten
                    try { ProfileMenuItem.Text = ProfileMenuText; } catch { }
                    try { LogoutMenuItem.Text = LogoutMenuText; } catch { }
                    try { QuickLabel.Text = QuickLabelText; } catch { }
                    try { QuickNewBookButton.Text = QuickNewBookButtonText; } catch { }
                    try { QuickNewMemberButton.Text = QuickNewMemberButtonText; } catch { }
                    try { FooterSettingsButton.Text = FooterSettingsText; } catch { }

                    // Update de FlyoutItem's Title (eerste item) als best-effort
                    try
                    {
                        var first = this.Items?.FirstOrDefault();
                        if (first is FlyoutItem fi) fi.Title = MenuTitle;
                    }
                    catch { }
                }
                catch { }
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
                    try { _languageService.LanguageChanged -= OnLanguageChangedExternally; } catch { }
                }
            }
            catch { }

            try
            {
                this.Navigated -= OnShellNavigated;
            }
            catch { }
        }

        private void UpdateThemeIcon()
        {
            try
            {
                var isDark = Application.Current?.UserAppTheme == AppTheme.Dark || Application.Current?.RequestedTheme == AppTheme.Dark;
                var src = isDark ? "moon.svg" : "sun.svg";

                // Zet de ImageSource direct vanuit de resource naam
                try
                {
                    ThemeToggleButton.ImageSource = ImageSource.FromFile(src);
                    ThemeToggleButton.Text = string.Empty; // zorg dat er geen tekst het icoon verbergt

                    // Verwijder ContentLayout toewijzing om type-resolutieproblemen tussen targets te vermijden
                    // ThemeToggleButton.ContentLayout = new ButtonContentLayout(ButtonContentLayout.ImagePosition.Left, 0);
                }
                catch (Exception imgEx)
                {
                    Debug.WriteLine($"Failed to set theme icon: {imgEx}");
                }
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

        // Reageer op taalwijzigingen die elders getriggerd zijn (of via de service)
        private void OnLanguageChangedExternally(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Gelokaliseerde titel en tekst
                        var title = Localize("LanguageChangedTitle");
                        var bodyTemplate = Localize("LanguageChangedBody");
                        var body = string.Format(bodyTemplate, culture.TwoLetterISOLanguageName.ToUpper());

                        try
                        {
                            await DisplayAlert(title, body, Localize("OK"));
                        }
                        catch { }

                        // Werk direct de gelokaliseerde flyout-titels bij zodat het menu de nieuwe taal toont
                        try { UpdateLocalizedFlyoutTitles(); } catch { }

                        // Zorg dat zichtbare pagina's / viewmodels hun gelokaliseerde strings verversen
                        try { RefreshVisiblePagesLocalizations(culture); } catch { }

                        // Maak de main page NIET opnieuw; abonnees op de language service moeten hun UI verversen.
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"OnLanguageChangedExternally handler error: {ex}");
                    }
                });
            }
            catch { }
        }

        // Probeer gelokaliseerde strings op zichtbare pagina's en hun viewmodels te verversen door veelgebruikte update-methodes via reflectie aan te roepen
        private void RefreshVisiblePagesLocalizations(System.Globalization.CultureInfo culture)
        {
#if DEBUG
            try { System.Diagnostics.Debug.WriteLine($"RefreshVisiblePagesLocalizations called for culture={culture.Name}"); } catch { }
#endif
            try
            {
                var pages = new List<Page>();

                // verzamel navigatiestack pagina's (pagina's gepusht op huidige navigatie)
                try
                {
                    var nav = this.Navigation ?? Shell.Current?.Navigation;
                    if (nav != null)
                    {
                        foreach (var p in nav.NavigationStack)
                        {
                            if (p != null && !pages.Contains(p)) pages.Add(p);
                        }
                        foreach (var p in nav.ModalStack)
                        {
                            if (p != null && !pages.Contains(p)) pages.Add(p);
                        }
                    }
                }
                catch { }

                // voeg ook de huidige Shell-pagina toe
                try
                {
                    var cur = Shell.Current?.CurrentPage;
                    if (cur != null && !pages.Contains(cur)) pages.Add(cur);
                }
                catch { }

                // Daarnaast: doorloop Shell items/sections/contents om ShellContent pagina's op te nemen
                try
                {
                    var shell = Shell.Current as Shell ?? this as Shell;
                    if (shell != null)
                    {
                        foreach (var item in shell.Items)
                        {
                            try
                            {
                                if (item is ShellItem shellItem)
                                {
                                    foreach (var section in shellItem.Items)
                                    {
                                        try
                                        {
                                            if (section is ShellSection shellSection)
                                            {
                                                // section.CurrentItem kan een ShellContent zijn
                                                var currentContent = shellSection.CurrentItem as ShellContent;
                                                if (currentContent != null)
                                                {
                                                    var contentPage = UnwrapPageFromShellContent(currentContent);
                                                    if (contentPage != null && !pages.Contains(contentPage)) pages.Add(contentPage);
                                                }

                                                // enumerateer ook alle contents in de section
                                                foreach (var content in shellSection.Items)
                                                {
                                                    try
                                                    {
                                                        if (content is ShellContent sc)
                                                        {
                                                            var p = UnwrapPageFromShellContent(sc);
                                                            if (p != null && !pages.Contains(p)) pages.Add(p);
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

#if DEBUG
                try { System.Diagnostics.Debug.WriteLine($"Pages to refresh: {string.Join(", ", pages.Select(p => p?.GetType()?.Name ?? "<null>"))}"); } catch { }
#endif

                foreach (var page in pages)
                {
                    try
                    {
#if DEBUG
                        try { System.Diagnostics.Debug.WriteLine($"Refreshing page: {page?.GetType()?.Name}"); } catch { }
#endif
                        // Geef voorkeur aan expliciete interface-aanroep wanneer geïmplementeerd op page
                        if (page is Biblio_App.Services.ILocalizable locPage)
                        {
                            try { locPage.UpdateLocalizedStrings();
#if DEBUG
                                try { System.Diagnostics.Debug.WriteLine($"Called ILocalizable.UpdateLocalizedStrings on page {page.GetType().Name}"); } catch { }
#endif
                            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error calling ILocalizable on page {page?.GetType()?.Name}: {ex}"); }
                        }
                        else
                        {
                            // Fallback reflectie-aanroep op page
                            var pageType = page.GetType();
                            var updPage = pageType.GetMethod("UpdateLocalizedStrings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (updPage != null)
                            {
                                try { updPage.Invoke(page, null);
#if DEBUG
                                    try { System.Diagnostics.Debug.WriteLine($"Invoked UpdateLocalizedStrings via reflection on page {pageType.Name}"); } catch { }
#endif
                                }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Reflection invocation error on page {pageType.Name}: {ex}"); }
                            }
                        }

                        // 2) Als BindingContext (viewmodel) ILocalizable implementeert, roep het direct aan
                        var bc = page.BindingContext;
                        if (bc is Biblio_App.Services.ILocalizable locVm)
                        {
                            try { locVm.UpdateLocalizedStrings();
#if DEBUG
                                try { System.Diagnostics.Debug.WriteLine($"Called ILocalizable.UpdateLocalizedStrings on ViewModel {bc.GetType().Name}"); } catch { }
#endif
                            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error calling ILocalizable on vm {bc?.GetType()?.Name}: {ex}"); }
                            continue;
                        }

                        // 3) Fallback naar reflectie om bekende refresh-methodes op viewmodel aan te roepen
                        if (bc != null)
                        {
                            var bcType = bc.GetType();
                            var refresh = bcType.GetMethod("RefreshLocalizedStrings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (refresh != null)
                            {
                                try { refresh.Invoke(bc, null);
#if DEBUG
                                    try { System.Diagnostics.Debug.WriteLine($"Invoked RefreshLocalizedStrings on ViewModel {bcType.Name}"); } catch { }
#endif
                                }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error invoking RefreshLocalizedStrings on vm {bcType.Name}: {ex}"); }
                                continue;
                            }

                            var upd = bcType.GetMethod("UpdateLocalizedStrings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (upd != null)
                            {
                                try { upd.Invoke(bc, null);
#if DEBUG
                                    try { System.Diagnostics.Debug.WriteLine($"Invoked UpdateLocalizedStrings on ViewModel {bcType.Name} via reflection"); } catch { }
#endif
                                }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error invoking UpdateLocalizedStrings on vm {bcType.Name}: {ex}"); }
                            }
                        }

                        // Zorg dat de paginalay-out ververst wordt zodat bindingen naar pagina-level BindableProperties toegepast worden
                        try
                        {
                            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try { page.ForceLayout(); page.InvalidateMeasure();
#if DEBUG
                                    try { System.Diagnostics.Debug.WriteLine($"Forced layout on page {page?.GetType()?.Name}"); } catch { }
#endif
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Layout refresh error on page {page?.GetType()?.Name}: {ex}");
                                }
                            });
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"RefreshVisiblePagesLocalizations error for page {page?.GetType()?.Name}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshVisiblePagesLocalizations top-level error: {ex}");
            }
        }

        // Helper om de echte Page uit ShellContent.Content wrappers zoals NavigationPage of TabbedPage te halen
        private Page? UnwrapPageFromShellContent(ShellContent sc)
        {
            try
            {
                var raw = sc?.Content;
                if (raw == null) return null;

                // Als content een NavigationPage is, geef bij voorkeur de CurrentPage
                if (raw is NavigationPage nav)
                {
                    return nav.CurrentPage ?? nav;
                }

                // Als content een TabbedPage is, geef bij voorkeur de CurrentPage
                if (raw is TabbedPage tp)
                {
                    return tp.CurrentPage ?? tp;
                }

                // Als content een Page is, retourneer die
                if (raw is Page p) return p;

                return null;
            }
            catch { return null; }
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

        // Runtime validatie helper om ontbrekende pages / null controls vroegtijdig te tonen.
        private void ValidatePages()
        {
            // Valideer dat geregistreerde pagetypes daadwerkelijk bestaan
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

            // Valideer dat belangrijke controls door XAML aangemaakt zijn
            var controls = new (string Name, object? Control)[]
            {
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

            // Basis sanity checks voor commands
            if (LoginCommand == null) Debug.WriteLine("LoginCommand is null.");
            if (OpenSettingsCommand == null) Debug.WriteLine("OpenSettingsCommand is null.");
        }
    }
}
