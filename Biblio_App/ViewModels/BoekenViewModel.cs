using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Biblio_Models.Entiteiten;
using Biblio_App.Services;
using System.ComponentModel;
using System;
using System.Linq;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biblio_App.Models.Pagination;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Globalization;
using Biblio_Models.Resources;

namespace Biblio_App.ViewModels
{
    public partial class BoekenViewModel : ObservableValidator, ILocalizable
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;
        private readonly IDataSyncService? _sync;
        private readonly ILanguageService? _languageService;

        private ResourceManager? _sharedResourceManager;
        private bool _resourceManagerInitialized = false;

        private CancellationTokenSource? _searchCts;

        public ObservableCollection<Boek> Boeken { get; } = new ObservableCollection<Boek>();
        public ObservableCollection<Categorie> Categorien { get; } = new ObservableCollection<Categorie>();

        [ObservableProperty]
        private Boek? selectedBoek;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        [StringLength(200, ErrorMessageResourceName = "StringLength", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private string titel = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        [StringLength(200, ErrorMessageResourceName = "StringLength", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private string auteur = string.Empty;

        [ObservableProperty]
        [StringLength(17, ErrorMessageResourceName = "StringLength", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private string isbn = string.Empty;

        [ObservableProperty]
        private int categorieId;

        // Filter/search properties
        [ObservableProperty]
        private string searchText = string.Empty;

        // Debounce search to avoid triggering LoadBooksAsync on every keystroke
        partial void OnSearchTextChanged(string value)
        {
            try
            {
                _searchCts?.Cancel();
                _searchCts = new CancellationTokenSource();
                var token = _searchCts.Token;

                // Fire-and-forget background task that waits a short debounce interval
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(400, token);
                        if (token.IsCancellationRequested) return;

                        // reset to first page and reload
                        Page = 1;
                        await LoadBooksAsync();
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex) { Debug.WriteLine(ex); }
                });
            }
            catch { }
        }

        [ObservableProperty]
        private int page = 1;

        [ObservableProperty]
        private int pageSize = 10;

        [ObservableProperty]
        private int totalPages;

        [ObservableProperty]
        private int totalCount;

        // Read-only display for the page indicator (e.g. "Pagina 1 / 5")
        public string PageDisplay => TotalPages > 0 ? $"{Localize("Page")} {Page} / {TotalPages}" : $"{Localize("Page")} {Page}";

        [ObservableProperty]
        private Categorie? selectedFilterCategorie;

        [ObservableProperty]
        private Categorie? selectedCategory;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        // per-field error properties
        public string TitelError => GetFirstError(nameof(Titel));
        public string AuteurError => GetFirstError(nameof(Auteur));
        public string IsbnError => GetFirstError(nameof(Isbn));

        public bool HasValidationErrors => !string.IsNullOrEmpty(ValidationMessage) || HasErrors;

        public IRelayCommand NieuwCommand { get; }
        public IAsyncRelayCommand OpslaanCommand { get; }
        public IAsyncRelayCommand VerwijderCommand { get; }
        public IAsyncRelayCommand ZoekCommand { get; }

        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand PrevPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }

        // item commands
        public IAsyncRelayCommand<Boek> ItemDetailsCommand { get; }
        public IAsyncRelayCommand<Boek> ItemEditCommand { get; }
        public IAsyncRelayCommand<Boek> ItemDeleteCommand { get; }

        // localized UI strings
        [ObservableProperty]
        private string pageTitle = string.Empty;

        [ObservableProperty]
        private string headerTitle = string.Empty;

        [ObservableProperty]
        private string headerAuthor = string.Empty;

        [ObservableProperty]
        private string headerActions = string.Empty;

        [ObservableProperty]
        private string searchPlaceholder = string.Empty;

        [ObservableProperty]
        private string searchButtonText = string.Empty;

        [ObservableProperty]
        private string categoryTitle = string.Empty;

        [ObservableProperty]
        private string newButtonText = string.Empty;

        [ObservableProperty]
        private string deleteButtonText = string.Empty;

        [ObservableProperty]
        private string saveButtonText = string.Empty;

        [ObservableProperty]
        private string isbnPlaceholder = string.Empty;

        [ObservableProperty]
        private string detailsButtonText = string.Empty;

        [ObservableProperty]
        private string editButtonText = string.Empty;

        [ObservableProperty]
        private string overviewText = string.Empty;

        [ObservableProperty]
        private string prevButtonText = "<<";

        [ObservableProperty]
        private string nextButtonText = ">>";

        [ObservableProperty]
        private int boekenCount;

        [ObservableProperty]
        private int ledenCount;

        [ObservableProperty]
        private int openUitleningenCount;

        public BoekenViewModel(IDbContextFactory<BiblioDbContext> dbFactory, IDataSyncService? sync = null, IGegevensProvider? gegevensProvider = null, ILanguageService? languageService = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _sync = sync;
            _gegevensProvider = gegevensProvider;
            _languageService = languageService;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);
            ZoekCommand = new AsyncRelayCommand(ZoekAsync);

            NextPageCommand = new AsyncRelayCommand(async () => { Page++; await LoadBooksAsync(); });
            PrevPageCommand = new AsyncRelayCommand(async () => { if (Page > 1) { Page--; await LoadBooksAsync(); } });
            GoToPageCommand = new AsyncRelayCommand<int>(async p => { if (p >= 1) { Page = p; await LoadBooksAsync(); } });

            // Navigate to a dedicated details page instead of showing an in-place popup
            ItemDetailsCommand = new AsyncRelayCommand<Boek>(async b => await NavigateToDetailsAsync(b));
            ItemEditCommand = new AsyncRelayCommand<Boek>(async b => await NavigateToEditAsync(b));
            ItemDeleteCommand = new AsyncRelayCommand<Boek>(async b => await DeleteItemAsync(b));

            // initialize localized strings
            UpdateLocalizedStrings();

            // Diagnostic: log resource names and lookup results to help debug missing translations
            try
            {
                LogResourceDiagnostics();
            }
            catch { }

            if (_languageService != null)
            {
                try
                {
                    // Ensure UI updates happen on the main thread when language changes
                    _languageService.LanguageChanged += (s, c) => Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
                }
                catch { }
            }

            // safely start background loads and observe exceptions
            RunSafe(LoadCategoriesAsync());
            RunSafe(LoadBooksAsync());
            RunSafe(LoadCountersAsync());
        }

        private void RunSafe(Task task)
        {
            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    try { Debug.WriteLine(t.Exception); } catch { }
                }
            }, TaskScheduler.Default);
        }

        private void EnsureResourceManagerInitialized()
        {
            if (_resourceManagerInitialized) return;
            _resourceManagerInitialized = true;

            try
            {
                var webType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Biblio_Web.SharedResource", false))
                    .FirstOrDefault(t => t != null);
                if (webType != null)
                {
                    var asm = webType.Assembly;
                    _sharedResourceManager = new ResourceManager("Biblio_Web.Resources.Vertalingen.SharedResource", asm);
#if DEBUG
                    try { Debug.WriteLine($"BoekenViewModel: using ResourceManager base='{_sharedResourceManager.BaseName}' from assembly='{asm.GetName().Name}'"); } catch { }
#endif
                    return;
                }
            }
            catch { }

            // Try to locate the Biblio_Web assembly explicitly and pick common resource base names.
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (asm == null)
                {
                    try { asm = Assembly.Load("Biblio_Web"); } catch { asm = null; }
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
                            // quick test to see if resource exists
                            var test = rm.GetString("Boeken", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
#if DEBUG
                                try { Debug.WriteLine($"BoekenViewModel: selected ResourceManager base='{baseName}' from assembly='{asm.GetName().Name}' (test key returned='{test}')"); } catch { }
#endif
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
                // Prefer the shared model resource's static ResourceManager to ensure a single source of truth
                _sharedResourceManager = Biblio_Models.Resources.SharedModelResource.ResourceManager;
#if DEBUG
                try { Debug.WriteLine($"BoekenViewModel: using SharedModelResource.ResourceManager base='{_sharedResourceManager.BaseName}'"); } catch { }
#endif
                return;
            }
            catch { }

            try
            {
                // Also try the MAUI app assembly's resources (Biblio_App) so added resx in the app project is discovered
                var appAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_App", StringComparison.OrdinalIgnoreCase));
                if (appAsm != null)
                {
                    var appCandidates = new[] {
                        "Biblio_App.Resources.Vertalingen.SharedResource",
                        "Biblio_App.Resources.SharedResource",
                        "Biblio_App.SharedResource"
                    };

                    foreach (var baseName in appCandidates)
                    {
                        try
                        {
                            var rm = new ResourceManager(baseName, appAsm);
                            var test = rm.GetString("Boeken", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
#if DEBUG
                                try { Debug.WriteLine($"BoekenViewModel: selected ResourceManager base='{baseName}' from assembly='{appAsm.GetName().Name}' (test key returned='{test}')"); } catch { }
#endif
                                return;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

#if DEBUG
            try
            {
                if (_sharedResourceManager == null)
                {
                    Debug.WriteLine("BoekenViewModel: No ResourceManager selected. Assemblies loaded:");
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try { Debug.WriteLine($"  assembly: {a.GetName().Name}, resources: {string.Join(",", a.GetManifestResourceNames().Take(10))}"); } catch { }
                    }
                }
            }
            catch { }
#endif
        }

        private string Localize(string key, string? arg = null)
        {
            EnsureResourceManagerInitialized();

            // If AppShell instance exists, prefer its localization logic so flyout and pages are consistent
            try
            {
                var shell = AppShell.Instance;
                if (shell != null)
                {
                    var locMethod = typeof(AppShell).GetMethod("Localize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (locMethod != null)
                    {
                        try
                        {
                            var result = locMethod.Invoke(shell, new object[] { key }) as string;
#if DEBUG
                            try { Debug.WriteLine($"BoekenViewModel.Localize: AppShell.Localize returned='{result}' for key='{key}'"); } catch { }
#endif
                            if (!string.IsNullOrEmpty(result)) return arg != null ? string.Format(System.Globalization.CultureInfo.CurrentUICulture, result, arg) : result;
                        }
                        catch { /* ignore reflection failures and fallback */ }
                    }
                }
            }
            catch { }

            var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
#if DEBUG
            try { Debug.WriteLine($"BoekenViewModel.Localize: using culture={culture.Name} and ResourceManager={( _sharedResourceManager != null ? _sharedResourceManager.BaseName : "<null>")}"); } catch { }
#endif
            if (_sharedResourceManager != null)
            {
                try
                {
                    var value = _sharedResourceManager.GetString(key, culture);
#if DEBUG
                    try { Debug.WriteLine($"BoekenViewModel.Localize: ResourceManager.GetString returned='{value}' for key='{key}'"); } catch { }
#endif
                    if (!string.IsNullOrEmpty(value)) return arg != null ? string.Format(culture, value, arg) : value;
                }
                catch { }
            }

            // fallback based on culture: English for 'en', otherwise Dutch
            var langCode = culture.TwoLetterISOLanguageName?.ToLowerInvariant() ?? string.Empty;
            if (langCode == "en")
            {
#if DEBUG
                try { Debug.WriteLine($"BoekenViewModel.Localize: falling back to hard-coded English for key='{key}'"); } catch { }
#endif
                return key switch
                {
                    "Boeken" => "Books",
                    "Books" => "Books",
                    "Titel" => "Title",
                    "Title" => "Title",
                    "Auteur" => "Author",
                    "Author" => "Author",
                    "Acties" => "Actions",
                    "ActiesHeader" => "Actions",
                    "Actie" => "Action",
                    "ZoekPlaceholder" => "Search by title, author or ISBN...",
                    "SearchPlaceholder" => "Search by title, author or ISBN...",
                    "Zoek" => "Search",
                    "Search" => "Search",
                    "Categorie" => "Category",
                    "Category" => "Category",
                    "Nieuw" => "New",
                    "New" => "New",
                    "Verwijderen" => "Delete",
                    "Delete" => "Delete",
                    "Opslaan" => "Save",
                    "Save" => "Save",
                    "ISBN" => "ISBN",
                    "Details" => "Details",
                    "Bewerk" => "Edit",
                    "Edit" => "Edit",
                    // overview text for books page
                    "BooksOverview" => "Overview of books — here you can view details, edit books and then add a new book",
                    "Alle" => "All",
                    "All" => "All",
                    "Page" => "Page",
                    "Prev" => "Previous",
                    "Next" => "Next",
                    _ => key
                };
            }

            if (langCode == "fr")
            {
#if DEBUG
                try { Debug.WriteLine($"BoekenViewModel.Localize: falling back to hard-coded French for key='{key}'"); } catch { }
#endif
                return key switch
                {
                    "Boeken" => "Livres",
                    "Books" => "Livres",
                    "Titel" => "Titre",
                    "Title" => "Titre",
                    "Auteur" => "Auteur",
                    "Author" => "Auteur",
                    "Acties" => "Actions",
                    "ActiesHeader" => "Actions",
                    "Actie" => "Action",
                    "ZoekPlaceholder" => "Rechercher par titre, auteur ou ISBN...",
                    "SearchPlaceholder" => "Rechercher par titre, auteur ou ISBN...",
                    "Zoek" => "Rechercher",
                    "Search" => "Rechercher",
                    "Categorie" => "Catégorie",
                    "Category" => "Catégorie",
                    "Nieuw" => "Nouveau",
                    "New" => "Nouveau",
                    "Verwijderen" => "Supprimer",
                    "Delete" => "Supprimer",
                    "Opslaan" => "Enregistrer",
                    "Save" => "Enregistrer",
                    "ISBN" => "ISBN",
                    "Details" => "Détails",
                    "Bewerk" => "Modifier",
                    "Edit" => "Modifier",
                    // overview text for books page (French)
                    "BooksOverview" => "Aperçu des livres — ici, vous pouvez voir les détails, modifier les livres et ensuite ajouter un nouveau livre",
                    "Alle" => "Tous",
                    "All" => "Tous",
                    "Page" => "Page",
                    "Prev" => "Précédent",
                    "Next" => "Suivant",
                    _ => key
                };
            }

            // default Dutch fallback — accept English keys too for robustness
#if DEBUG
            try { Debug.WriteLine($"BoekenViewModel.Localize: falling back to hard-coded Dutch for key='{key}'"); } catch { }
#endif
            return key switch
            {
                "Boeken" => "Boeken",
                "Books" => "Boeken",
                "Titel" => "Titel",
                "Title" => "Titel",
                "Auteur" => "Auteur",
                "Author" => "Auteur",
                "Acties" => "Acties",
                "Actie" => "Actie",
                "ZoekPlaceholder" => "Zoek op titel, auteur of ISBN...",
                "SearchPlaceholder" => "Zoek op titel, auteur of ISBN...",
                "Zoek" => "Zoek",
                "Search" => "Zoek",
                "Categorie" => "Categorie",
                "Category" => "Categorie",
                "Nieuw" => "Nieuw",
                "New" => "Nieuw",
                "Verwijderen" => "Verwijderen",
                "BooksOverview" => "Overzicht van boeken — hier kun je details bekijken, boeken bewerken en daarna een nieuw boek toevoegen",
                "Delete" => "Verwijderen",
                "Opslaan" => "Opslaan",
                "Save" => "Opslaan",
                "ISBN" => "ISBN",
                "Details" => "Details",
                "Bewerk" => "Bewerk",
                "Edit" => "Bewerk",
                "Alle" => "Alle",
                "All" => "Alle",
                "Page" => "Pagina",
                "Prev" => "Vorige",
                "Next" => "Volgende",
                _ => key
            };
        }

        // ensure this public method satisfies ILocalizable; the implementation reuses existing logic
        // Public method to satisfy ILocalizable — call the core implementation without debug break.
        public void UpdateLocalizedStrings()
        {
            UpdateLocalizedStringsCore();
        }

        // Rename the previous UpdateLocalizedStrings implementation to a core method
        private void UpdateLocalizedStringsCore()
        {
            // Use shared resource keys (English) so AppShell and viewmodels use the same keys
            PageTitle = Localize("Books");
            HeaderTitle = Localize("Title");
            HeaderAuthor = Localize("Author");
            HeaderActions = Localize("Actions");
            SearchPlaceholder = Localize("SearchPlaceholder");
            SearchButtonText = Localize("Search");
            CategoryTitle = Localize("Category");
            NewButtonText = Localize("New");
            DeleteButtonText = Localize("Delete");
            SaveButtonText = Localize("Save");
            IsbnPlaceholder = Localize("ISBN");
            DetailsButtonText = Localize("Details");
            EditButtonText = Localize("Edit");
            OverviewText = Localize("BooksOverview");

            // Prev/Next paging button labels
            PrevButtonText = Localize("Prev");
            NextButtonText = Localize("Next");

            // Ensure computed/display-only properties are refreshed when language changes
            OnPropertyChanged(nameof(PageDisplay));
        }

        // update constructor and RefreshLocalizedStrings to call the public UpdateLocalizedStrings
        public void RefreshLocalizedStrings()
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
            }
            catch { }
        }

        partial void OnSelectedBoekChanged(Boek? value)
        {
            if (value != null)
            {
                Titel = value.Titel;
                Auteur = value.Auteur;
                Isbn = value.Isbn;
                CategorieId = value.CategorieID;
                SelectedCategory = Categorien.FirstOrDefault(c => c.Id == value.CategorieID);
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
            else
            {
                Titel = string.Empty;
                Auteur = string.Empty;
                Isbn = string.Empty;
                CategorieId = 0;
                SelectedCategory = null;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
        }

        partial void OnSelectedCategoryChanged(Categorie? value)
        {
            if (value != null) CategorieId = value.Id;
        }

        private async Task LoadCategoriesAsync()
        {
            var localList = new List<Categorie>();
            localList.Add(new Categorie { Id = 0, Naam = "Alle" });
            // prefer sync service when available
            if (_sync is IDataSyncService sync)
            {
                try
                {
                    var cats = await sync.GetCategorieenAsync(true);
                    foreach (var c in cats) localList.Add(c);

                    // update collection on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Categorien.Clear();
                        foreach (var c in localList) Categorien.Add(c);
                        SelectedFilterCategorie = Categorien.FirstOrDefault();
                    });

                    return;
                }
                catch { /* fallback to local */ }
            }

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var cats = await db.Categorien.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync();
                foreach (var c in cats) localList.Add(c);
            }
            catch { }

            // finalize update on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Categorien.Clear();
                foreach (var c in localList) Categorien.Add(c);
                SelectedFilterCategorie = Categorien.FirstOrDefault();
            });
        }

        // Ensure categories are loaded (used by pages when navigated with query params)
        public async Task EnsureCategoriesLoadedAsync()
        {
            if (Categorien == null || Categorien.Count == 0)
            {
                await LoadCategoriesAsync();
            }
        }

        // Validation helper - mirrors LedenViewModel
        private string GetFirstError(string propertyName)
        {
            var notifier = (System.ComponentModel.INotifyDataErrorInfo)this;
            var errors = notifier.GetErrors(propertyName) as IEnumerable;
            if (errors != null)
            {
                foreach (var e in errors) if (e != null) return e.ToString();
            }
            return string.Empty;
        }

        private void RaiseFieldErrorProperties()
        {
            OnPropertyChanged(nameof(TitelError));
            OnPropertyChanged(nameof(AuteurError));
            OnPropertyChanged(nameof(IsbnError));
            OnPropertyChanged(nameof(HasValidationErrors));
        }

        private async Task LoadCountersAsync()
        {
            try
            {
                int boekenCountLocal = 0;
                int ledenCountLocal = 0;
                int openUitleningenLocal = 0;

                if (_gegevensProvider != null)
                {
                    try
                    {
                        var t = await _gegevensProvider.GetTellersAsync();
                        boekenCountLocal = t.boeken;
                        ledenCountLocal = t.leden;
                        openUitleningenLocal = t.openUitleningen;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            BoekenCount = boekenCountLocal;
                            LedenCount = ledenCountLocal;
                            OpenUitleningenCount = openUitleningenLocal;
                        });

                        return;
                    }
                    catch { }
                }

                using var db = _dbFactory.CreateDbContext();
                try
                {
                    boekenCountLocal = await db.Boeken.CountAsync(b => !b.IsDeleted);
                }
                catch { boekenCountLocal = 0; }

                try
                {
                    ledenCountLocal = await db.Leden.CountAsync();
                }
                catch { ledenCountLocal = 0; }

                try
                {
                    openUitleningenLocal = await db.Leningens.CountAsync(l => l.ReturnedAt == null);
                }
                catch { openUitleningenLocal = 0; }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BoekenCount = boekenCountLocal;
                    LedenCount = ledenCountLocal;
                    OpenUitleningenCount = openUitleningenLocal;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task LoadBooksAsync()
        {
            try
            {
                Debug.WriteLine($"LoadBooksAsync starting: Page={Page}, PageSize={PageSize}, SearchText='{SearchText}', SelectedFilterCategorie={(SelectedFilterCategorie?.Id.ToString() ?? "null")}" );

                // If a sync service is available prefer it (it will attempt API then update local store)
                if (_sync is IDataSyncService syncSvc)
                {
                    try
                    {
                        var all = await syncSvc.GetBoekenAsync(true);
                        Debug.WriteLine($"DataSyncService returned {all?.Count ?? 0} items.");

                        var filtered = (all ?? new List<Boek>()).Where(b => !b.IsDeleted);

                        if (!string.IsNullOrWhiteSpace(SearchText))
                        {
                            var s = SearchText.Trim().ToLowerInvariant();
                            filtered = filtered.Where(b => (b.Titel ?? string.Empty).ToLower().Contains(s)
                                || (b.Auteur ?? string.Empty).ToLower().Contains(s)
                                || (b.Isbn ?? string.Empty).ToLower().Contains(s));
                        }

                        if (SelectedFilterCategorie != null && SelectedFilterCategorie.Id != 0)
                        {
                            var catId = SelectedFilterCategorie.Id;
                            filtered = filtered.Where(b => b.CategorieID == catId);
                        }

                        var listAll = filtered.OrderBy(b => b.Titel).ToList();
                        var total = listAll.Count;

                        var pageItems = listAll.Skip((Page - 1) * PageSize).Take(PageSize).ToList();

                        // Resolve category names on background thread first
                        foreach (var b in pageItems)
                        {
                            try { b.CategorieNaam = await ResolveCategoryNameAsync(b.CategorieID); } catch { }
                        }

                        // Update UI-bound properties and collection on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            TotalCount = total;
                            TotalPages = PageSize > 0 ? (int)Math.Ceiling(total / (double)PageSize) : 1;

                            Boeken.Clear();
                            foreach (var b in pageItems) Boeken.Add(b);
                        });

                        return;
                    }
                    catch { /* fallback to local loader below */ }
                }

                // fallback to local DB
                using var db = _dbFactory.CreateDbContext();
                var query = db.Boeken.AsNoTracking().Where(b => !b.IsDeleted).AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var s = SearchText.Trim().ToLowerInvariant();
                    query = query.Where(b => (b.Titel ?? string.Empty).ToLower().Contains(s)
                        || (b.Auteur ?? string.Empty).ToLower().Contains(s)
                        || (b.Isbn ?? string.Empty).ToLower().Contains(s));
                }

                if (SelectedFilterCategorie != null && SelectedFilterCategorie.Id != 0)
                {
                    var catId = SelectedFilterCategorie.Id;
                    query = query.Where(b => b.CategorieID == catId);
                }

                var totalLocal = await query.CountAsync();
                var items = await query.OrderBy(b => b.Titel).Skip((Page - 1) * PageSize).Take(PageSize).ToListAsync();

                // Resolve category names before touching UI collection
                foreach (var b in items)
                {
                    try { b.CategorieNaam = await ResolveCategoryNameAsync(b.CategorieID); } catch { }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalCount = totalLocal;
                    TotalPages = PageSize > 0 ? (int)Math.Ceiling(totalLocal / (double)PageSize) : 1;

                    Boeken.Clear();
                    foreach (var b in items) Boeken.Add(b);
                });

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void Nieuw()
        {
            SelectedBoek = null;
            Titel = string.Empty;
            Auteur = string.Empty;
            Isbn = string.Empty;
            CategorieId = 0;
            SelectedCategory = null;
            ValidationMessage = string.Empty;
            ClearErrors();
            RaiseFieldErrorProperties();
        }

        private async Task ZoekAsync()
        {
            Page = 1;
            await LoadBooksAsync();
        }

        private async Task OpslaanAsync()
        {
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors)
            {
                // build validation message
                var props = new[] { nameof(Titel), nameof(Auteur), nameof(Isbn) };
                var messages = new List<string>();
                var notifier = (System.ComponentModel.INotifyDataErrorInfo)this;
                foreach (var p in props)
                {
                    var errors = notifier.GetErrors(p) as IEnumerable;
                    if (errors != null)
                    {
                        foreach (var e in errors) messages.Add(e?.ToString());
                    }
                }

                ValidationMessage = string.Join("\n", messages.Where(m => !string.IsNullOrWhiteSpace(m)));
                RaiseFieldErrorProperties();
                await ShowAlertAsync("Validatie", ValidationMessage);
                return;
            }

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var query = db.Boeken.AsNoTracking().Where(b => !b.IsDeleted).AsQueryable();
                if (SelectedBoek == null)
                {
                    var nieuw = new Boek
                    {
                        Titel = Titel,
                        Auteur = Auteur,
                        Isbn = Isbn,
                        CategorieID = CategorieId
                    };
                    db.Boeken.Add(nieuw);
                }
                else
                {
                    var existing = await db.Boeken.FindAsync(SelectedBoek.Id);
                    if (existing != null)
                    {
                        existing.Titel = Titel;
                        existing.Auteur = Auteur;
                        existing.Isbn = Isbn;
                        existing.CategorieID = CategorieId;
                        db.Boeken.Update(existing);
                    }
                }

                await db.SaveChangesAsync();
                await LoadBooksAsync();

                SelectedBoek = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Boek opgeslagen.");
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij opslaan.";
                Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
            }
        }

        private async Task VerwijderAsync()
        {
            ValidationMessage = string.Empty;
            if (SelectedBoek == null) return;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Boeken.FindAsync(SelectedBoek.Id);
                if (existing != null)
                {
                    db.Boeken.Remove(existing);
                    await db.SaveChangesAsync();
                }

                await LoadBooksAsync();
                SelectedBoek = null;
                await ShowAlertAsync("Gereed", "Boek verwijderd.");
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij verwijderen.";
                Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
            }
        }

        private async Task DeleteItemAsync(Boek? item)
        {
            if (item == null) return;
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Boeken.FindAsync(item.Id);
                if (existing != null)
                {
                    db.Boeken.Remove(existing);
                    await db.SaveChangesAsync();
                }
                await LoadBooksAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task NavigateToDetailsAsync(Boek? b)
        {
            if (b == null) return;
            try
            {
                await Shell.Current.GoToAsync($"{nameof(Pages.BoekDetailsPage)}?boekId={b.Id}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", "Kan detailspagina niet openen.");
            }
        }

        private async Task NavigateToEditAsync(Boek? b)
        {
            if (b == null) return;
            try
            {
                await Shell.Current.GoToAsync($"{nameof(Pages.BoekDetailsPage)}?boekId={b.Id}&edit=true");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", "Kan bewerkpagina niet openen.");
            }
        }

        private async Task ShowAlertAsync(string title, string message)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(title, message, "OK");
                    }
                });
            }
            catch { }
        }

        private async Task ShowDetailsAsync(Boek? b)
        {
            if (b == null) return;
            try
            {
                var isbnLabel = Localize("ISBN");
                var categoryLabel = Localize("Category");
                var title = Localize("Details");

                // try to resolve category name from loaded categories
                string categoryName = string.Empty;
                try
                {
                    var cat = Categorien?.FirstOrDefault(c => c.Id == b.CategorieID);
                    if (cat != null && !string.IsNullOrWhiteSpace(cat.Naam))
                    {
                        categoryName = cat.Naam;
                      }
                }
                catch { }

                // if not found in-memory, attempt a DB lookup
                if (string.IsNullOrWhiteSpace(categoryName) && b.CategorieID != 0)
                {
                    try
                    {
                        using var db = _dbFactory.CreateDbContext();
                        var dbCat = await db.Categorien.AsNoTracking().FirstOrDefaultAsync(c => c.Id == b.CategorieID);
                        if (dbCat != null && !string.IsNullOrWhiteSpace(dbCat.Naam)) categoryName = dbCat.Naam;
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(categoryName)) categoryName = b.CategorieID != 0 ? b.CategorieID.ToString() : Localize("Alle");

                var body = $"{b.Titel}\n{b.Auteur}\n{isbnLabel}: {b.Isbn}\n{categoryLabel}: {categoryName}";
                await ShowAlertAsync(title, body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task<string> ResolveCategoryNameAsync(int categoryId)
        {
            try
            {
                if (categoryId == 0) return Localize("Alle");

                var inMem = Categorien?.FirstOrDefault(c => c.Id == categoryId);
                if (inMem != null && !string.IsNullOrWhiteSpace(inMem.Naam)) return inMem.Naam;

                try
                {
                    using var db = _dbFactory.CreateDbContext();
                    var dbCat = await db.Categorien.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);
                    if (dbCat != null && !string.IsNullOrWhiteSpace(dbCat.Naam)) return dbCat.Naam;
                }
                catch { }

                return categoryId.ToString();
            }
            catch { return categoryId.ToString(); }
        }

        // Diagnostic helper: list assemblies' embedded resources and test GetString on common resource base names
        private void LogResourceDiagnostics()
        {
            try
            {
                Debug.WriteLine("--- Resource diagnostics start ---");
                var assembliesToCheck = new[] { "Biblio_App", "Biblio_Web", "Biblio_Models" };
                foreach (var asmName in assembliesToCheck)
                {
                    try
                    {
                        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, asmName, StringComparison.OrdinalIgnoreCase));
                        if (asm == null)
                        {
                            try { asm = Assembly.Load(asmName); } catch { asm = null; }
                        }

                        if (asm == null)
                        {
                            Debug.WriteLine($"Assembly not loaded: {asmName}");
                            continue;
                        }

                        Debug.WriteLine($"Assembly '{asm.GetName().Name}' loaded. Manifest resources: {string.Join(", ", asm.GetManifestResourceNames())}");

                        var candidateBases = new[] {
                            $"{asm.GetName().Name}.Resources.Vertalingen.SharedResource",
                            $"{asm.GetName().Name}.Resources.SharedResource",
                            $"{asm.GetName().Name}.SharedResource"
                        };

                        foreach (var baseName in candidateBases)
                        {
                            try
                            {
                                var rm = new ResourceManager(baseName, asm);
                                var val = rm.GetString("BooksOverview", CultureInfo.CurrentUICulture);
                                Debug.WriteLine($"ResourceManager lookup (base='{baseName}') -> BooksOverview='{val ?? "<null>"}'");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"ResourceManager creation/lookup failed for base '{baseName}': {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error inspecting assembly {asmName}: {ex}");
                    }
                }

                // Also test AppShell translation helper if available
                try
                {
                    var shell = AppShell.Instance;
                    if (shell != null)
                    {
                        var t = shell.Translate("BooksOverview");
                        Debug.WriteLine($"AppShell.Translate('BooksOverview') -> '{t}'");
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"AppShell.Translate check failed: {ex}"); }

                // Finally log current Culture
                Debug.WriteLine($"CurrentUICulture: {CultureInfo.CurrentUICulture.Name}, CurrentCulture: {CultureInfo.CurrentCulture.Name}");
                Debug.WriteLine("--- Resource diagnostics end ---");
            }
            catch (Exception ex)
            {
                try { Debug.WriteLine($"LogResourceDiagnostics error: {ex}"); } catch { }
            }
        }
    }
}