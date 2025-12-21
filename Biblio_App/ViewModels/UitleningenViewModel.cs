using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using Biblio_App.Services;
using Biblio_Models.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.Globalization;
using System.Reflection;
using Biblio_Models.Resources;
using System.IO;
using System.Xml.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel : ObservableValidator, Biblio_App.Services.ILocalizable
    {
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;
        private readonly ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;
        private Dictionary<string, string>? _resxFileStrings; // fallback loaded from web resx on disk
        private bool _resourceManagerInitialized = false;

        public ObservableCollection<Lenen> Uitleningen { get; } = new ObservableCollection<Lenen>();
        public ObservableCollection<Boek> BoekenList { get; } = new ObservableCollection<Boek>();
        public ObservableCollection<Lid> LedenList { get; } = new ObservableCollection<Lid>();
        public ObservableCollection<Categorie> Categorieen { get; } = new ObservableCollection<Categorie>();

        [ObservableProperty]
        private Lenen? selectedUitlening;

        // localized UI strings
        [ObservableProperty]
        private string pageHeaderText = string.Empty;
        [ObservableProperty]
        private string membersLabel = string.Empty;
        [ObservableProperty]
        private string loansLabel = string.Empty;
        [ObservableProperty]
        private string booksLabel = string.Empty;
        [ObservableProperty]
        private string dbPathLabelText = string.Empty;
        [ObservableProperty]
        private string copyPathButtonText = string.Empty;
        [ObservableProperty]
        private string searchPlaceholderText = string.Empty;
        [ObservableProperty]
        private string filterButtonText = string.Empty;
        [ObservableProperty]
        private string categoryTitle = string.Empty;
        [ObservableProperty]
        private string memberTitle = string.Empty;
        [ObservableProperty]
        private string bookTitle = string.Empty;
        [ObservableProperty]
        private string onlyOpenText = string.Empty;
        [ObservableProperty]
        private string viewButtonText = string.Empty;
        [ObservableProperty]
        private string returnButtonText = string.Empty;
        [ObservableProperty]
        private string newButtonText = string.Empty;
        [ObservableProperty]
        private string saveButtonText = string.Empty;
        [ObservableProperty]
        private string deleteButtonText = string.Empty;

        // additional localized labels
        [ObservableProperty]
        private string startLabel = string.Empty;
        [ObservableProperty]
        private string dueLabel = string.Empty;
        [ObservableProperty]
        private string returnedLabel = string.Empty;
        [ObservableProperty]
        private string dbPathNotLoadedText = string.Empty;

        // Return status selection (UI picker)
        public System.Collections.ObjectModel.ObservableCollection<string> ReturnStatusOptions { get; } = new System.Collections.ObjectModel.ObservableCollection<string>();
        [ObservableProperty]
        private string selectedReturnStatus = string.Empty;

        // existing properties remain
        [ObservableProperty]
        private Boek? selectedBoek;

        [ObservableProperty]
        private Lid? selectedLid;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private DateTime startDate = DateTime.Now;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private DateTime dueDate = DateTime.Now.AddDays(14);

        [ObservableProperty]
        private DateTime? returnedAt;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        // filter/search
        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private Categorie? selectedCategory;

        [ObservableProperty]
        private bool onlyOpen;

        // Additional UI filter properties
        [ObservableProperty]
        private Lid? filterLid;

        partial void OnFilterLidChanged(Lid? value)
        {
            _ = LoadDataWithFiltersAsync();
        }

        [ObservableProperty]
        private Boek? filterBoek;

        partial void OnFilterBoekChanged(Boek? value)
        {
            _ = LoadDataWithFiltersAsync();
        }

        [ObservableProperty]
        private bool showLate;

        partial void OnShowLateChanged(bool value)
        {
            _ = LoadDataWithFiltersAsync();
        }

        // Sorting
        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>();
        [ObservableProperty]
        private string sortOption = "StartDate";
        partial void OnSortOptionChanged(string value)
        {
            _ = LoadDataWithFiltersAsync();
        }

        [ObservableProperty]
        private bool sortDescending = true;
        partial void OnSortDescendingChanged(bool value)
        {
            _ = LoadDataWithFiltersAsync();
        }

        [ObservableProperty]
        private string lastError = string.Empty;

        [ObservableProperty]
        private string selectedFilter = "Alle";

        public int LedenCount => LedenList?.Count ?? 0;
        public int UitleningenCount => Uitleningen?.Count ?? 0;
        public int BoekenCount => BoekenList?.Count ?? 0;

        // Path to local sqlite DB used by the app
        public string DbPath => Path.Combine(FileSystem.AppDataDirectory, "biblio.db");

        // Combined debug info shown on the UI (counts + db path)
        public string DebugInfo => $"Leden: {LedenCount}  Uitleningen: {UitleningenCount}  Boeken: {BoekenCount}\nDB: {DbPath}";

        // per-field errors (keep BoekId/LidId errors but also show object-based)
        public string BoekError => SelectedBoek == null ? "" : string.Empty; // placeholder, main errors via ValidationMessage
        public string LidError => SelectedLid == null ? "" : string.Empty;
        public string StartDateError => GetFirstError(nameof(StartDate));
        public string DueDateError => GetFirstError(nameof(DueDate));

        public bool HasValidationErrors => !string.IsNullOrEmpty(ValidationMessage) || HasErrors;

        public IRelayCommand NieuwCommand { get; }
        public IAsyncRelayCommand OpslaanCommand { get; }
        public IAsyncRelayCommand VerwijderCommand { get; }

        public IRelayCommand ZoekCommand { get; }
        public IRelayCommand<string> SetFilterCommand { get; }
        public IAsyncRelayCommand<Lenen?> ReturnCommand { get; }
        public IAsyncRelayCommand<Lenen?> DeleteCommand { get; }

        public IAsyncRelayCommand SyncCommand => new AsyncRelayCommand(async () => await ExecuteSyncAsync());

        public UitleningenViewModel(IDbContextFactory<BiblioDbContext> dbFactory, ILanguageService? languageService = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _languageService = languageService;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            ZoekCommand = new RelayCommand(async () => await LoadDataWithFiltersAsync());
            ReturnCommand = new AsyncRelayCommand<Lenen?>(ReturnAsync);
            DeleteCommand = new AsyncRelayCommand<Lenen?>(DeleteAsync);

            SetFilterCommand = new RelayCommand<string>(async (p) =>
            {
                if (string.IsNullOrWhiteSpace(p)) return;
                SelectedFilter = p;
                await LoadDataWithFiltersAsync();
            });

            // listen for members changes to refresh members list
            try { Microsoft.Maui.Controls.MessagingCenter.Subscribe<LedenViewModel>(this, "MembersChanged", async (vm) => { await LoadMembersAsync(); }); } catch { }

            // initialize localized strings
            UpdateLocalizedStrings();
            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += (s, c) => Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
                }
            }
            catch { }

            SortOptions.Add("StartDate");
            SortOptions.Add("DueDate");
            SortOptions.Add("Lid");
            SortOptions.Add("Boek");

            // Do not load data directly from constructor; this can block startup/UI thread on some devices.
            // Pages should call InitializeAsync/EnsureDataLoadedAsync during OnAppearing.
        }

        private bool _initialized = false;
        private bool _dbPathResolved = false;

        // Public initializer to be called from the page (OnAppearing)
        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await EnsureDataLoadedAsync();

                // After data has been loaded, try to resolve the actual DB file path
                try
                {
                    var appDir = FileSystem.AppDataDirectory;
                    var candidate1 = Path.Combine(appDir, "biblio.db");
                    var candidate2 = Path.Combine(appDir, "BiblioApp.db");
                    string? existing = null;
                    if (File.Exists(candidate1)) existing = candidate1;
                    else if (File.Exists(candidate2)) existing = candidate2;

                    if (!string.IsNullOrEmpty(existing))
                    {
                        DbPathNotLoadedText = existing;
                        _dbPathResolved = true;
                    }
                    else
                    {
                        // keep localized fallback when no file exists yet
                        DbPathNotLoadedText = Localize("DbPathNotLoaded");
                    }

                    OnPropertyChanged(nameof(DbPathNotLoadedText));
                    OnPropertyChanged(nameof(DebugInfo));
                }
                catch { }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task LoadMembersAsync()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var leden = await db.Leden.AsNoTracking().OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
                LedenList.Clear();
                foreach (var l in leden) LedenList.Add(l);
                OnPropertyChanged(nameof(LedenCount));
                OnPropertyChanged(nameof(DebugInfo));
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        private void EnsureResourceManagerInitialized()
        {
            if (_resourceManagerInitialized) return;
            _resourceManagerInitialized = true;

            try
            {
                // Prefer the MAUI app's own resources first
                var appAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_App", StringComparison.OrdinalIgnoreCase));
                if (appAsm != null)
                {
                    foreach (var name in new[] { "Biblio_App.Resources.Vertalingen.SharedResource", "Biblio_App.Resources.SharedResource", "Biblio_App.SharedResource" })
                    {
                        try
                        {
                            var rm = new ResourceManager(name, appAsm);
                            var test = rm.GetString("Members", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                // Next prefer shared model resource (Biblio_Models)
                if (_sharedResourceManager == null)
                {
                    try
                    {
                        var modelType = typeof(SharedModelResource);
                        if (modelType != null)
                        {
                            _sharedResourceManager = Biblio_Models.Resources.SharedModelResource.ResourceManager;
                        }
                    }
                    catch { }
                }

                // Finally try web project's resources as fallback
                if (_sharedResourceManager == null)
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
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // try loading resx files from repo as fallback (development)
            TryLoadResxFromRepo();
        }

        private void TryLoadResxFromRepo()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "Biblio_Web", "Resources", "Vertalingen");
                    if (Directory.Exists(candidate))
                    {
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

        public string Localize(string key)
        {
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
                            var fromShell = locMethod.Invoke(shell, new object[] { key }) as string;
                            if (!string.IsNullOrEmpty(fromShell)) return fromShell;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            EnsureResourceManagerInitialized();
            var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
            if (_sharedResourceManager != null)
            {
                try
                {
                    var val = _sharedResourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(val)) return val;
                }
                catch { }
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
                    "ReturnedOption" => "Returned",
                    "ReturnedLabel" => "Return status",
                    "New" => "New",
                    "Save" => "Save",
                    "Delete" => "Delete",
                    "StartLabel" => "Start:",
                    "DueLabel" => "Due:",
                    "DbPathNotLoaded" => "(not loaded)",
                    "Validation" => "Validation",
                    "Ready" => "Ready",
                    "Error" => "Error",
                    "SavedLoan" => "Loan saved.",
                    "DeletedLoan" => "Loan deleted.",
                    "BookMarkedReturned" => "Book marked as returned.",
                    "Validation_BookRequired" => "Book is required.",
                    "Validation_MemberRequired" => "Member is required.",
                    "ErrorSavingLoan" => "Error saving loan.",
                    "ErrorDeletingLoan" => "Error deleting loan.",
                    "ErrorUpdatingLoan" => "Error updating loan.",
                    "OK" => "OK",
                    _ => key
                };
            }

            if (code == "fr")
            {
                return key switch
                {
                    "Members" => "Membres",
                    "Loans" => "Prêts",
                    "Books" => "Livres",
                    "DbPath" => "Chemin DB:",
                    "CopyPath" => "Copier le chemin",
                    "SearchPlaceholder" => "Rechercher...",
                    "Filter" => "Filtrer",
                    "Category" => "Catégorie",
                    "Member" => "Membre",
                    "Book" => "Livre",
                    "OnlyOpen" => "Seulement ouverts",
                    "View" => "Voir",
                    "Return" => "Retourner",
                    "ReturnedOption" => "Rendu",
                    "ReturnedLabel" => "Statut de livraison",
                    "New" => "Nouveau",
                    "Save" => "Enregistrer",
                    "Delete" => "Supprimer",
                    "StartLabel" => "Début:",
                    "DueLabel" => "Échéance:",
                    "DbPathNotLoaded" => "(non chargé)",
                    "Validation" => "Validation",
                    "Ready" => "Terminé",
                    "Error" => "Erreur",
                    "SavedLoan" => "Prêt enregistré.",
                    "DeletedLoan" => "Prêt supprimé.",
                    "BookMarkedReturned" => "Livre marqué comme rendu.",
                    "Validation_BookRequired" => "Le livre est obligatoire.",
                    "Validation_MemberRequired" => "Le membre est obligatoire.",
                    "ErrorSavingLoan" => "Erreur lors de l'enregistrement du prêt.",
                    "ErrorDeletingLoan" => "Erreur lors de la suppression du prêt.",
                    "ErrorUpdatingLoan" => "Erreur lors de la mise à jour du prêt.",
                    "OK" => "OK",
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
                "ReturnedOption" => "Geleverd",
                "ReturnedLabel" => "Lever status",
                "New" => "Nieuw",
                "Save" => "Opslaan",
                "Delete" => "Verwijder",
                "StartLabel" => "Start:",
                "DueLabel" => "Tot:",
                "DbPathNotLoaded" => "(niet geladen)",
                "Validation" => "Validatie",
                "Ready" => "Gereed",
                "Error" => "Fout",
                "SavedLoan" => "Uitlening opgeslagen.",
                "DeletedLoan" => "Uitlening verwijderd.",
                "BookMarkedReturned" => "Boek als ingeleverd gemarkeerd.",
                "Validation_BookRequired" => "Boek is verplicht.",
                "Validation_MemberRequired" => "Lid is verplicht.",
                "ErrorSavingLoan" => "Fout bij opslaan uitlening.",
                "ErrorDeletingLoan" => "Kan uitlening niet verwijderen; mogelijk gekoppeld.",
                "ErrorUpdatingLoan" => "Kan uitlening niet updaten.",
                "OK" => "OK",
                "Late" => "Te laat",
                _ => key
            };
        }

        // public UpdateLocalizedStrings to satisfy ILocalizable
        public void UpdateLocalizedStrings()
        {
            // ensure resource manager available
            try { EnsureResourceManagerInitialized(); } catch { }

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

            // debug what was resolved so we can see problems in Output window
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UitleningenViewModel] Localized ReturnButtonText='{ReturnButtonText}', DeleteButtonText='{DeleteButtonText}'");
            }
            catch { }

            // ensure visible fallbacks
            if (string.IsNullOrWhiteSpace(ReturnButtonText)) ReturnButtonText = "Inleveren";
            if (string.IsNullOrWhiteSpace(DeleteButtonText)) DeleteButtonText = "Verwijderen";

            // notify UI that these properties changed
            try
            {
                OnPropertyChanged(nameof(ReturnButtonText));
                OnPropertyChanged(nameof(DeleteButtonText));
            }
            catch { }

            // additional labels
            StartLabel = Localize("StartLabel");
            DueLabel = Localize("DueLabel");
            ReturnedLabel = Localize("ReturnedLabel");
            // If localization failed (returned key or empty), provide explicit per-culture fallback
            if (string.IsNullOrWhiteSpace(ReturnedLabel) || string.Equals(ReturnedLabel, "ReturnedLabel", StringComparison.OrdinalIgnoreCase))
            {
                var code = _languageService?.CurrentCulture?.TwoLetterISOLanguageName ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                ReturnedLabel = (code ?? "nl").ToLowerInvariant() switch
                {
                    "en" => "Return status",
                    "fr" => "Statut de livraison",
                    _ => "Lever status",
                };
            }

            if (!_dbPathResolved)
            {
                DbPathNotLoadedText = Localize("DbPathNotLoaded");
            }

            try
            {
                ReturnStatusOptions.Clear();
                var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;

                var optReturn = Localize("Return");
                var optDelivered = Localize("ReturnedOption");
                var optLate = Localize("Late");

                // If localization returned the key name (fallback failed) or an unexpected value, provide explicit fallbacks per culture
                if (string.IsNullOrWhiteSpace(optDelivered) || string.Equals(optDelivered, "ReturnedOption", StringComparison.OrdinalIgnoreCase) || optDelivered.IndexOf("ReturnedOption", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                    optDelivered = code switch
                    {
                        "fr" => "Rendu",
                        "en" => "Returned",
                        _ => "Geleverd"
                    };
                }

                if (string.IsNullOrWhiteSpace(optLate) || string.Equals(optLate, "Late", StringComparison.OrdinalIgnoreCase) || optLate.IndexOf("Late", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                    optLate = code switch
                    {
                        "fr" => "En retard",
                        "en" => "Late",
                        _ => "Te laat"
                    };
                }

                ReturnStatusOptions.Add(optReturn); // not returned (Inleveren)
                ReturnStatusOptions.Add(optDelivered); // delivered/ingeleverd
                ReturnStatusOptions.Add(optLate); // late

                if (string.IsNullOrWhiteSpace(SelectedReturnStatus)) SelectedReturnStatus = ReturnStatusOptions.FirstOrDefault();
            }
            catch { }

            // notify computed/derived properties
            OnPropertyChanged(nameof(PageHeaderText));
            OnPropertyChanged(nameof(MembersLabel));
            OnPropertyChanged(nameof(LoansLabel));
            OnPropertyChanged(nameof(BooksLabel));
        }

        // keep the rest of the existing methods unchanged
        private void Nieuw() => SelectedUitlening = null;

        private void RaiseCountProperties()
        {
            OnPropertyChanged(nameof(LedenCount));
            OnPropertyChanged(nameof(UitleningenCount));
            OnPropertyChanged(nameof(BoekenCount));
            OnPropertyChanged(nameof(DebugInfo));
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var uit = await db.Leningens.Include(l => l.Boek).Include(l => l.Lid).AsNoTracking().OrderByDescending(l => l.StartDate).ToListAsync();
                Uitleningen.Clear();
                foreach (var u in uit)
                {
                    Uitleningen.Add(u);
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] Loan {u.Id}: BoekId={u.BoekId} LidId={u.LidId} ReturnedAt={u.ReturnedAt} LidFull={(u.Lid == null ? "NULL" : u.Lid.FullName)}");
                    }
                    catch { }
                }

                var boeken = await db.Boeken.AsNoTracking().Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();
                BoekenList.Clear();
                foreach (var b in boeken) BoekenList.Add(b);

                var leden = await db.Leden.AsNoTracking().OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
                LedenList.Clear();
                foreach (var l in leden) LedenList.Add(l);

                var cats = await db.Categorien.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync();
                Categorieen.Clear();
                Categorieen.Add(new Categorie { Id = 0, Naam = "Alle" });
                foreach (var c in cats) Categorieen.Add(c);

                SelectedCategory = Categorieen.FirstOrDefault();

                // reset last error on successful load
                LastError = string.Empty;

                // update counts
                RaiseCountProperties();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Debug.WriteLine(ex);
                RaiseCountProperties();
            }
        }

        public async Task EnsureDataLoadedAsync()
        {
            await LoadDataAsync();
        }

        public async Task LoadDataWithFiltersAsync()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var query = db.Leningens.Include(l => l.Boek).Include(l => l.Lid).AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var s = SearchText.Trim().ToLowerInvariant();

                    if (string.Equals(SelectedFilter, "Lid", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(l => l.Lid != null && (((l.Lid.Voornaam ?? "") + " " + (l.Lid.AchterNaam ?? "")).ToLower().Contains(s)
                            || (l.Lid.Email ?? "").ToLower().Contains(s)));
                    }
                    else if (string.Equals(SelectedFilter, "Boek", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(l => l.Boek != null && (((l.Boek.Titel ?? "").ToLower().Contains(s))
                            || ((l.Boek.Auteur ?? "").ToLower().Contains(s))));
                    }
                    else
                    {
                        // Alle (default) - search across both
                        query = query.Where(l => (l.Boek != null && (l.Boek.Titel ?? string.Empty).ToLower().Contains(s))
                            || (l.Boek != null && (l.Boek.Auteur ?? string.Empty).ToLower().Contains(s))
                            || (l.Lid != null && (((l.Lid.Voornaam ?? string.Empty) + " " + (l.Lid.AchterNaam ?? string.Empty)).ToLower().Contains(s)))
                            || (l.Lid != null && (l.Lid.Email ?? string.Empty).ToLower().Contains(s)));
                    }
                }

                if (SelectedCategory != null && SelectedCategory.Id != 0)
                {
                    var catId = SelectedCategory.Id;
                    query = query.Where(l => l.Boek != null && l.Boek.CategorieID == catId);
                }

                // Only open filter
                if (OnlyOpen)
                {
                    query = query.Where(l => l.ReturnedAt == null);
                }

                // Filter by selected member/book
                if (FilterLid != null)
                {
                    query = query.Where(l => l.LidId == FilterLid.Id);
                }

                if (FilterBoek != null)
                {
                    query = query.Where(l => l.BoekId == FilterBoek.Id);
                }

                if (ShowLate)
                {
                    var today = DateTime.UtcNow.Date;
                    query = query.Where(l => l.DueDate < today && l.ReturnedAt == null);
                }

                // Sorting
                switch ((SortOption ?? "StartDate").ToLowerInvariant())
                {
                    case "lid":
                        query = SortDescending ? query.OrderByDescending(l => l.Lid.Voornaam).ThenByDescending(l => l.Lid.AchterNaam) : query.OrderBy(l => l.Lid.Voornaam).ThenBy(l => l.Lid.AchterNaam);
                        break;
                    case "boek":
                        query = SortDescending ? query.OrderByDescending(l => l.Boek.Titel) : query.OrderBy(l => l.Boek.Titel);
                        break;
                    case "duedate":
                        query = SortDescending ? query.OrderByDescending(l => l.DueDate) : query.OrderBy(l => l.DueDate);
                        break;
                    default:
                        query = SortDescending ? query.OrderByDescending(l => l.StartDate) : query.OrderBy(l => l.StartDate);
                        break;
                }

                var list = await query.ToListAsync();
                Uitleningen.Clear();
                foreach (var u in list) Uitleningen.Add(u);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void BuildValidationMessage()
        {
            var props = new[] { nameof(StartDate), nameof(DueDate) };
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

            if (SelectedBoek == null) messages.Add(Localize("Validation_BookRequired"));
            if (SelectedLid == null) messages.Add(Localize("Validation_MemberRequired"));

            ValidationMessage = string.Join("\n", messages.Where(m => !string.IsNullOrWhiteSpace(m)));
            RaiseFieldErrorProperties();
        }

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
            OnPropertyChanged(nameof(BoekError));
            OnPropertyChanged(nameof(LidError));
            OnPropertyChanged(nameof(StartDateError));
            OnPropertyChanged(nameof(DueDateError));
            OnPropertyChanged(nameof(HasValidationErrors));
        }

        private async Task OpslaanAsync()
        {
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors || SelectedBoek == null || SelectedLid == null)
            {
                BuildValidationMessage();
                await ShowAlertAsync(Localize("Validation"), ValidationMessage);
                return;
            }

            try
            {
                try
                {
                    if (string.Equals(SelectedReturnStatus, Localize("ReturnedOption"), StringComparison.OrdinalIgnoreCase))
                    {
                        ReturnedAt = DateTime.Now;
                    }
                    else
                    {
                        ReturnedAt = null;
                    }
                }
                catch { }

                using var db = _dbFactory.CreateDbContext();

                Lenen? savedEntity = null;

                if (SelectedUitlening == null)
                {
                    var nieuw = new Lenen
                    {
                        BoekId = SelectedBoek.Id,
                        LidId = SelectedLid.Id,
                        StartDate = StartDate,
                        DueDate = DueDate,
                        ReturnedAt = ReturnedAt
                    };

                    db.Leningens.Add(nieuw);
                    await db.SaveChangesAsync();

                    try
                    {
                        savedEntity = await db.Leningens
                            .Include(l => l.Boek)
                            .Include(l => l.Lid)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(l => l.Id == nieuw.Id);
                    }
                    catch { }
                }
                else
                {
                    var existing = await db.Leningens.FindAsync(SelectedUitlening.Id);
                    if (existing != null)
                    {
                        existing.BoekId = SelectedBoek.Id;
                        existing.LidId = SelectedLid.Id;
                        existing.StartDate = StartDate;
                        existing.DueDate = DueDate;
                        existing.ReturnedAt = ReturnedAt;
                        db.Leningens.Update(existing);
                        await db.SaveChangesAsync();

                        try
                        {
                            savedEntity = await db.Leningens
                                .Include(l => l.Boek)
                                .Include(l => l.Lid)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(l => l.Id == existing.Id);
                        }
                        catch { }
                    }
                }

                // Update the in-memory collection so the UI updates immediately (icons/labels reflect change)
                try
                {
                    if (savedEntity != null)
                    {
                        // Ensure UI-only flags reflect the currently selected return status so converters show correct icon/label
                        try
                        {
                            var isLate = string.Equals(SelectedReturnStatus, Localize("Late"), StringComparison.OrdinalIgnoreCase);
                            var isReturn = string.Equals(SelectedReturnStatus, Localize("Return"), StringComparison.OrdinalIgnoreCase);
                            savedEntity.ForceLate = isLate && !savedEntity.ReturnedAt.HasValue;
                            savedEntity.ForceNotLate = isReturn && !savedEntity.ReturnedAt.HasValue;
                        }
                        catch { }

                        var existingIndex = Uitleningen.ToList().FindIndex(u => u.Id == savedEntity.Id);
                        if (existingIndex >= 0)
                        {
                            Uitleningen[existingIndex] = savedEntity;
                        }
                        else
                        {
                            Uitleningen.Insert(0, savedEntity);
                        }

                        RaiseCountProperties();
                    }
                }
                catch { }

                SelectedUitlening = savedEntity;
                ValidationMessage = string.Empty;
                await ShowAlertAsync(Localize("Ready"), Localize("SavedLoan"));
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorSavingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorSavingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
        }

        private async Task VerwijderAsync()
        {
            if (SelectedUitlening == null) return;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leningens.FindAsync(SelectedUitlening.Id);
                if (existing != null)
                {
                    db.Leningens.Remove(existing);
                    await db.SaveChangesAsync();
                }

                await LoadDataAsync();
                SelectedUitlening = null;
                await ShowAlertAsync(Localize("Ready"), Localize("DeletedLoan"));
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorDeletingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorDeletingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
        }

        private async Task DeleteAsync(Lenen? item)
        {
            if (item == null) return;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leningens.FindAsync(item.Id);
                if (existing != null)
                {
                    db.Leningens.Remove(existing);
                    await db.SaveChangesAsync();
                }

                await LoadDataAsync();
                await ShowAlertAsync(Localize("Ready"), Localize("DeletedLoan"));
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorDeletingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorDeletingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
        }

        private IEnumerable<string> GetRolesFromToken()
        {
            try
            {
                var auth = App.Current?.Handler?.MauiContext?.Services?.GetService<Biblio_App.Services.IAuthService>();
                var token = auth?.GetToken();
                if (string.IsNullOrWhiteSpace(token)) return Enumerable.Empty<string>();

                try { System.Diagnostics.Debug.WriteLine($"[GetRolesFromToken] raw token: {token}"); } catch { }

                var parts = token.Split('.');
                if (parts.Length < 2) return Enumerable.Empty<string>();
                var payload = parts[1];
                // pad base64
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var roles = new List<string>();

                // Helper to add string or array values
                void AddFromElement(System.Text.Json.JsonElement el)
                {
                    try
                    {
                        if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var v = el.GetString();
                            if (!string.IsNullOrWhiteSpace(v)) roles.Add(v);
                        }
                        else if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in el.EnumerateArray())
                            {
                                if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var v = item.GetString();
                                    if (!string.IsNullOrWhiteSpace(v)) roles.Add(v);
                                }
                            }
                        }
                        else if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            // try to find nested 'roles' property
                            foreach (var p in el.EnumerateObject())
                            {
                                if (p.Name.IndexOf("role", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    AddFromElement(p.Value);
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Direct known properties
                try
                {
                    if (doc.RootElement.TryGetProperty("role", out var r)) AddFromElement(r);
                    if (doc.RootElement.TryGetProperty("roles", out var rr)) AddFromElement(rr);
                    if (doc.RootElement.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var schemaRole)) AddFromElement(schemaRole);
                }
                catch { }

                // Enumerate all root properties and pick anything with 'role' in the name
                try
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Name.IndexOf("role", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            AddFromElement(prop.Value);
                        }
                        else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            // check nested objects for roles (common with Keycloak: realm_access.roles)
                            foreach (var nested in prop.Value.EnumerateObject())
                            {
                                if (nested.Name.IndexOf("role", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    AddFromElement(nested.Value);
                                }
                            }
                        }
                    }
                }
                catch { }

                // Specific Keycloak-style path
                try
                {
                    if (doc.RootElement.TryGetProperty("realm_access", out var realm) && realm.ValueKind == System.Text.Json.JsonValueKind.Object && realm.TryGetProperty("roles", out var realmRoles))
                    {
                        AddFromElement(realmRoles);
                    }
                }
                catch { }

                var result = roles.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                try { System.Diagnostics.Debug.WriteLine($"[GetRolesFromToken] detected roles: {string.Join(",", result)}"); } catch { }
                return result;
            }
            catch { }
            return Enumerable.Empty<string>();
        }

        // Fallback: check if the JWT payload contains a substring (case-insensitive).
        private bool TokenPayloadContains(string substring)
        {
            try
            {
                var auth = App.Current?.Handler?.MauiContext?.Services?.GetService<Biblio_App.Services.IAuthService>();
                var token = auth?.GetToken();
                if (string.IsNullOrWhiteSpace(token)) return false;
                var parts = token.Split('.');
                if (parts.Length < 2) return false;
                var payload = parts[1];
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                return json?.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { }
            return false;
        }

        private bool UserHasRole(params string[] requiredRoles)
        {
            try
            {
                var roles = GetRolesFromToken();
                foreach (var r in requiredRoles)
                {
                    if (roles.Any(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase))) return true;
                }
            }
            catch { }
            return false;
        }

        private async Task ReturnAsync(Lenen? item)
        {
            if (item == null) return;

            try
            {
                // Only allow admins and medewerkers to perform return
                if (!UserHasRole("Admin", "Medewerker"))
                {
                    // fallback: allow if JWT payload contains 'admin' (many tokens put roles in non-standard places)
                    if (!TokenPayloadContains("admin"))
                    {
                        await ShowAlertAsync(Localize("Error"), "Niet gemachtigd om in te leveren.");
                        return;
                    }
                }

                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leningens.FindAsync(item.Id);
                if (existing != null)
                {
                    existing.ReturnedAt = DateTime.Now;
                    db.Leningens.Update(existing);
                    await db.SaveChangesAsync();
                }

                await LoadDataAsync();
                await ShowAlertAsync(Localize("Ready"), Localize("BookMarkedReturned"));
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorUpdatingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = Localize("ErrorUpdatingLoan");
                await ShowAlertAsync(Localize("Error"), ValidationMessage);
            }
        }

        private async Task ExecuteSyncAsync()
        {
            try
            {
                // Resolve IDataSyncService from the current MAUI DI container
                var ds = App.Current?.Handler?.MauiContext?.Services?.GetService<IDataSyncService>();
                if (ds == null)
                {
                    LastError = "No sync service available.";
                    return;
                }

                await ds.SyncAllAsync();
                // reload local data after sync
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
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
                        await Application.Current.MainPage.DisplayAlert(title, message, Localize("OK"));
                    }
                });
            }
            catch
            {
                // ignore
            }
        }

        partial void OnSelectedUitleningChanged(Lenen? value)
        {
            try
            {
                if (value == null)
                {
                    SelectedReturnStatus = ReturnStatusOptions.FirstOrDefault();
                    SelectedBoek = null;
                    SelectedLid = null;
                    StartDate = DateTime.Now;
                    DueDate = DateTime.Now.AddDays(14);
                    ReturnedAt = null;
                    return;
                }

                // populate form fields from the selected loan
                try
                {
                    // Map to existing items in the lists (use Id) so Pickers show the correct SelectedItem
                    if (value.Boek != null)
                    {
                        var matchBoek = BoekenList.FirstOrDefault(b => b.Id == value.Boek.Id);
                        SelectedBoek = matchBoek ?? value.Boek;
                    }
                    else
                    {
                        SelectedBoek = null;
                    }

                    if (value.Lid != null)
                    {
                        var matchLid = LedenList.FirstOrDefault(l => l.Id == value.Lid.Id);
                        SelectedLid = matchLid ?? value.Lid;
                    }
                    else
                    {
                        SelectedLid = null;
                    }

                    StartDate = value.StartDate;
                    DueDate = value.DueDate;
                    ReturnedAt = value.ReturnedAt;
                }
                catch { }

                if (value.ReturnedAt.HasValue)
                {
                    // map to the picker option for delivered
                    SelectedReturnStatus = Localize("ReturnedOption");
                    try { value.ForceLate = false; } catch { }
                    try { value.ForceNotLate = false; } catch { }
                }
                else if (value.ForceLate)
                {
                    // preserve explicit UI override for Late
                    SelectedReturnStatus = Localize("Late");
                    try { value.ForceLate = true; } catch { }
                    try { value.ForceNotLate = false; } catch { }
                }
                else if (value.ForceNotLate)
                {
                    // preserve explicit UI override for Return
                    SelectedReturnStatus = Localize("Return");
                    try { value.ForceLate = false; } catch { }
                    try { value.ForceNotLate = true; } catch { }
                }
                else if (value.DueDate < DateTime.Now.Date)
                {
                    SelectedReturnStatus = Localize("Late");
                    try { value.ForceLate = true; } catch { }
                    try { value.ForceNotLate = false; } catch { }
                }
                else
                {
                    SelectedReturnStatus = Localize("Return");
                    try { value.ForceLate = false; } catch { }
                    try { value.ForceNotLate = true; } catch { }
                }
            }
            catch { }
        }

        // When the SelectedReturnStatus in the form changes, update the in-memory loan and refresh the list icons immediately
        partial void OnSelectedReturnStatusChanged(string value)
        {
            try
            {
                if (SelectedUitlening == null) return;

                // Map status to ReturnedAt without saving to DB immediately
                // 'ReturnedOption' => set ReturnedAt; 'Late' or 'Return' => keep ReturnedAt null
                DateTime? newReturnedAt = null;
                if (string.Equals(value, Localize("ReturnedOption"), StringComparison.OrdinalIgnoreCase))
                {
                    newReturnedAt = DateTime.Now;
                }

                // Update viewmodel property
                ReturnedAt = newReturnedAt;

                // Update the selected loan object and force collection replace so UI rebinds and converters update icons/labels
                try
                {
                    SelectedUitlening.ReturnedAt = newReturnedAt;
                    // set ForceLate/ForceNotLate according to selected value so icons update immediately
                    try
                    {
                        var isLate = string.Equals(value, Localize("Late"), StringComparison.OrdinalIgnoreCase);
                        var isReturn = string.Equals(value, Localize("Return"), StringComparison.OrdinalIgnoreCase);
                        SelectedUitlening.ForceLate = isLate && !newReturnedAt.HasValue;
                        SelectedUitlening.ForceNotLate = isReturn && !newReturnedAt.HasValue;
                    }
                    catch { }
                    var idx = Uitleningen.IndexOf(SelectedUitlening);
                    if (idx >= 0)
                    {
                        // Replace item to raise CollectionChanged (Replace) so DataTemplate re-evaluates bindings/converters
                        Uitleningen[idx] = SelectedUitlening;
                      }
                }
                catch { }

                // Persist change to database in background so UI updates immediately and DB stays in sync
                try
                {
                    _ = PersistReturnedStatusAsync(SelectedUitlening, newReturnedAt);
                }
                catch { }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task PersistReturnedStatusAsync(Lenen item, DateTime? newReturnedAt)
        {
            try
            {
                if (item == null || item.Id == 0) return;

                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leningens.FindAsync(item.Id);
                if (existing != null)
                {
                    existing.ReturnedAt = newReturnedAt;
                    db.Leningens.Update(existing);
                    await db.SaveChangesAsync();

                    // reload with includes so navigation props are populated
                    var saved = await db.Leningens.Include(l => l.Boek).Include(l => l.Lid).AsNoTracking().FirstOrDefaultAsync(l => l.Id == existing.Id);
                    if (saved != null)
                    {
                        try
                        {
                            // update collection on main thread
                            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var idx = Uitleningen.ToList().FindIndex(u => u.Id == saved.Id);

                                // Preserve UI override flags (Late/Return) so the picker/labels/icons reflect user's choice
                                try
                                {
                                    var isLate = string.Equals(SelectedReturnStatus, Localize("Late"), StringComparison.OrdinalIgnoreCase);
                                    var isReturn = string.Equals(SelectedReturnStatus, Localize("Return"), StringComparison.OrdinalIgnoreCase);
                                    saved.ForceLate = isLate && !saved.ReturnedAt.HasValue;
                                    saved.ForceNotLate = isReturn && !saved.ReturnedAt.HasValue;
                                }
                                catch { }

                                if (idx >= 0) Uitleningen[idx] = saved;
                                else Uitleningen.Insert(0, saved);

                                // keep SelectedUitlening in sync
                                SelectedUitlening = saved;
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}