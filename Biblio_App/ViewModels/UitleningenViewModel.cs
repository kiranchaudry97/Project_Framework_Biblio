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

            // Do not load data directly from constructor; this can block startup/UI thread on some devices.
            // Pages should call InitializeAsync/EnsureDataLoadedAsync during OnAppearing.
        }

        private bool _initialized = false;

        // Public initializer to be called from the page (OnAppearing)
        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await EnsureDataLoadedAsync();
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

        private string Localize(string key)
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
                    "New" => "New",
                    "Save" => "Save",
                    "Delete" => "Delete",
                    "StartLabel" => "Start:",
                    "DueLabel" => "Due:",
                    "ReturnedLabel" => "Returned:",
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
                    "New" => "Nouveau",
                    "Save" => "Enregistrer",
                    "Delete" => "Supprimer",
                    "StartLabel" => "Début:",
                    "DueLabel" => "Échéance:",
                    "ReturnedLabel" => "Rendu:",
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
                "New" => "Nieuw",
                "Save" => "Opslaan",
                "Delete" => "Verwijder",
                "StartLabel" => "Start:",
                "DueLabel" => "Tot:",
                "ReturnedLabel" => "Ingeleverd:",
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
            DbPathNotLoadedText = Localize("DbPathNotLoaded");

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
                foreach (var u in uit) Uitleningen.Add(u);

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

        private async Task LoadDataWithFiltersAsync()
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

                if (OnlyOpen)
                {
                    query = query.Where(l => l.ReturnedAt == null);
                }

                var list = await query.OrderByDescending(l => l.StartDate).ToListAsync();
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
                using var db = _dbFactory.CreateDbContext();
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
                    }
                }

                await db.SaveChangesAsync();

                await LoadDataAsync();
                SelectedUitlening = null;
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

        private async Task ReturnAsync(Lenen? item)
        {
            if (item == null) return;

            try
            {
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
    }
}