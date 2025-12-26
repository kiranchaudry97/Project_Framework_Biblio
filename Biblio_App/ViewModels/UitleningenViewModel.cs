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
        private readonly IDbContextFactory<LocalDbContext> _dbFactory;
        private readonly ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;
        private Dictionary<string, string>? _resxFileStrings; // fallback geladen van web resx op schijf
        private bool _resourceManagerInitialized = false;

        public ObservableCollection<Lenen> Uitleningen { get; } = new ObservableCollection<Lenen>();
        public ObservableCollection<Boek> BoekenList { get; } = new ObservableCollection<Boek>();
        public ObservableCollection<Lid> LedenList { get; } = new ObservableCollection<Lid>();
        public ObservableCollection<Categorie> Categorieen { get; } = new ObservableCollection<Categorie>();

        [ObservableProperty]
        private Lenen? selectedUitlening;

        // gelokaliseerde UI strings
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

        // extra gelokaliseerde labels
        [ObservableProperty]
        private string startLabel = string.Empty;
        [ObservableProperty]
        private string dueLabel = string.Empty;
        [ObservableProperty]
        private string returnedLabel = string.Empty;
        [ObservableProperty]
        private string dbPathNotLoadedText = string.Empty;

        // Inlever status selectie (UI picker)
        public System.Collections.ObjectModel.ObservableCollection<string> ReturnStatusOptions { get; } = new System.Collections.ObjectModel.ObservableCollection<string>();
        [ObservableProperty]
        private string selectedReturnStatus = string.Empty;

        // bestaande eigenschappen blijven behouden
        [ObservableProperty]
        private Boek? selectedBoek;

        [ObservableProperty]
        private Lid? selectedLid;

    // Edit form visibility property
    public bool IsEditFormVisible => SelectedUitlening != null || SelectedBoek != null || SelectedLid != null;

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

        // filter/zoeken
        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private Categorie? selectedCategory;

        [ObservableProperty]
        private bool onlyOpen;

        // Extra UI filter eigenschappen
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

        // Sorteren
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

        // Pad naar lokale sqlite DB gebruikt door de app
        public string DbPath => Path.Combine(FileSystem.AppDataDirectory, "biblio.db");

        // Gecombineerde debug info getoond op de UI (aantallen + db pad)
        public string DebugInfo => $"Leden: {LedenCount}  Uitleningen: {UitleningenCount}  Boeken: {BoekenCount}\nDB: {DbPath}";

        // per-veld fouten (behoud BoekId/LidId fouten maar toon ook object-gebaseerd)
        public string BoekError => SelectedBoek == null ? "" : string.Empty; // placeholder, hoofdfouten via ValidationMessage
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

        public UitleningenViewModel(IDbContextFactory<LocalDbContext> dbFactory, ILanguageService? languageService = null)
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

            // luister naar ledenwijzigingen om ledenlijst te vernieuwen
            try { Microsoft.Maui.Controls.MessagingCenter.Subscribe<LedenViewModel>(this, "MembersChanged", async (vm) => { await LoadMembersAsync(); }); } catch { }

            // initialiseer gelokaliseerde strings
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

            // Laad data niet direct vanuit de constructor; dit kan opstarten/UI thread blokkeren op sommige apparaten.
            // Pagina's moeten InitializeAsync/EnsureDataLoadedAsync aanroepen tijdens OnAppearing.
        }

        private bool _initialized = false;
        private bool _dbPathResolved = false;

        // Publieke initializer om aan te roepen vanuit de pagina (OnAppearing)
        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await EnsureDataLoadedAsync();

                // Nadat data is geladen, probeer het werkelijke DB bestandspad op te lossen
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
                        // behoud gelokaliseerde fallback wanneer er nog geen bestand bestaat
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
                
                // MUST update ObservableCollection on Main Thread for Android
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        LedenList.Clear();
                        foreach (var l in leden) LedenList.Add(l);
                        OnPropertyChanged(nameof(LedenCount));
                        OnPropertyChanged(nameof(DebugInfo));
                    }
                    catch (Exception ex)
                    {
                        LastError = ex.Message;
                    }
                });
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
                // Geef voorkeur aan de MAUI app's eigen resources eerst
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

                // Geef vervolgens voorkeur aan gedeelde model resource (Biblio_Models)
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

                // Probeer tenslotte web project's resources als fallback
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

            // probeer resx bestanden te laden van repo als fallback (ontwikkeling)
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

        // publieke UpdateLocalizedStrings om ILocalizable te voldoen
        public void UpdateLocalizedStrings()
        {
            // zorg ervoor dat resource manager beschikbaar is
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

            // debug wat er is opgelost zodat we problemen kunnen zien in Output venster
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UitleningenViewModel] Localized ReturnButtonText='{ReturnButtonText}', DeleteButtonText='{DeleteButtonText}'");
            }
            catch { }

            // zorg voor zichtbare fallbacks
            if (string.IsNullOrWhiteSpace(ReturnButtonText)) ReturnButtonText = "Inleveren";
            if (string.IsNullOrWhiteSpace(DeleteButtonText)) DeleteButtonText = "Verwijderen";

            // meld UI dat deze eigenschappen zijn gewijzigd
            try
            {
                OnPropertyChanged(nameof(ReturnButtonText));
                OnPropertyChanged(nameof(DeleteButtonText));
            }
            catch { }

            // extra labels
            StartLabel = Localize("StartLabel");
            DueLabel = Localize("DueLabel");
            ReturnedLabel = Localize("ReturnedLabel");
            // Als lokalisatie mislukt (sleutel of leeg geretourneerd), geef expliciete per-cultuur fallback
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

                // Als lokalisatie de sleutelnaam heeft geretourneerd (fallback mislukt) of een onverwachte waarde, geef expliciete fallbacks per cultuur
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

                ReturnStatusOptions.Add(optReturn); // niet ingeleverd (Inleveren)
                ReturnStatusOptions.Add(optDelivered); // geleverd/ingeleverd
                ReturnStatusOptions.Add(optLate); // te laat

                if (string.IsNullOrWhiteSpace(SelectedReturnStatus)) SelectedReturnStatus = ReturnStatusOptions.FirstOrDefault();
            }
            catch { }

            // meld berekende/afgeleide eigenschappen
            OnPropertyChanged(nameof(PageHeaderText));
            OnPropertyChanged(nameof(MembersLabel));
            OnPropertyChanged(nameof(LoansLabel));
            OnPropertyChanged(nameof(BooksLabel));
        }

        // behoud de rest van de bestaande methoden ongewijzigd
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
                var boeken = await db.Boeken.AsNoTracking().Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();
                var leden = await db.Leden.AsNoTracking().OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
                var cats = await db.Categorien.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync();

                // MUST update ObservableCollections on Main Thread for Android
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
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

                        BoekenList.Clear();
                        foreach (var b in boeken) BoekenList.Add(b);

                        LedenList.Clear();
                        foreach (var l in leden) LedenList.Add(l);

                        Categorieen.Clear();
                        Categorieen.Add(new Categorie { Id = 0, Naam = "Alle" });
                        foreach (var c in cats) Categorieen.Add(c);

                        SelectedCategory = Categorieen.FirstOrDefault();

                        // reset laatste fout bij succesvol laden
                        LastError = string.Empty;

                        // update aantallen
                        RaiseCountProperties();
                    }
                    catch (Exception ex)
                    {
                        LastError = ex.Message;
                        Debug.WriteLine(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Debug.WriteLine(ex);
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => RaiseCountProperties());
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
                        // Alle (standaard) - zoek in beide
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

                // Alleen open filter
                if (OnlyOpen)
                {
                    query = query.Where(l => l.ReturnedAt == null);
                }

                // Filter op geselecteerd lid/boek
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

                // Sorteren
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
                
                // MUST update ObservableCollection on Main Thread for Android
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        Uitleningen.Clear();
                        foreach (var u in list) Uitleningen.Add(u);
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadDataWithFiltersAsync UI update error: {innerEx}");
                    }
                });
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

                // Update de in-memory collectie zodat de UI direct wordt bijgewerkt (iconen/labels reflecteren wijziging)
                try
                {
                    if (savedEntity != null)
                    {
                        // Zorg ervoor dat UI-only vlaggen de huidige geselecteerde inlever status weerspiegelen zodat converters het juiste icoon/label tonen
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
                // vul base64 aan
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var roles = new List<string>();

                // Helper om string of array waarden toe te voegen
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
                            // probeer geneste 'roles' eigenschap te vinden
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

                // Direct bekende eigenschappen
                try
                {
                    if (doc.RootElement.TryGetProperty("role", out var r)) AddFromElement(r);
                    if (doc.RootElement.TryGetProperty("roles", out var rr)) AddFromElement(rr);
                    if (doc.RootElement.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var schemaRole)) AddFromElement(schemaRole);
                }
                catch { }

                // Doorloop alle root eigenschappen en pak alles met 'role' in de naam
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
                            // controleer geneste objecten voor roles (gebruikelijk bij Keycloak: realm_access.roles)
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

                // Specifiek Keycloak-stijl pad
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

        // Fallback: controleer of de JWT payload een substring bevat (hoofdletterongevoelig).
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
                // Sta alleen admins en medewerkers toe om in te leveren
                if (!UserHasRole("Admin", "Medewerker"))
                {
                    // fallback: sta toe als JWT payload 'admin' bevat (veel tokens plaatsen roles op niet-standaard plaatsen)
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
                // Los IDataSyncService op vanuit de huidige MAUI DI container
                var ds = App.Current?.Handler?.MauiContext?.Services?.GetService<IDataSyncService>();
                if (ds == null)
                {
                    LastError = "No sync service available.";
                    return;
                }

                await ds.SyncAllAsync();
                // herlaad lokale data na synchronisatie
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
                // negeer
            }
        }

        // Wanneer de SelectedReturnStatus in het formulier wijzigt, update de in-memory uitlening en ververs de lijst iconen direct
        partial void OnSelectedReturnStatusChanged(string value)
        {
            try
            {
                if (SelectedUitlening == null) return;

                // Map status naar ReturnedAt zonder direct op te slaan in DB
                // 'ReturnedOption' => zet ReturnedAt; 'Late' of 'Return' => houd ReturnedAt null
                DateTime? newReturnedAt = null;
                if (string.Equals(value, Localize("ReturnedOption"), StringComparison.OrdinalIgnoreCase))
                {
                    newReturnedAt = DateTime.Now;
                }

                // Update viewmodel eigenschap
                ReturnedAt = newReturnedAt;

                // Update het geselecteerde uitlening object en forceer collectie vervanging zodat UI opnieuw bindt en converters iconen/labels updaten
                try
                {
                    SelectedUitlening.ReturnedAt = newReturnedAt;
                    // zet ForceLate/ForceNotLate volgens geselecteerde waarde zodat iconen direct updaten
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
                        // Vervang item om CollectionChanged (Replace) te triggeren zodat DataTemplate bindings/converters opnieuw evalueert
                        Uitleningen[idx] = SelectedUitlening;
                      }
                }
                catch { }

                // Persisteer wijziging naar database op de achtergrond zodat UI direct update en DB gesynchroniseerd blijft
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

                    // herlaad met includes zodat navigatie eigenschappen zijn gevuld
                    var saved = await db.Leningens.Include(l => l.Boek).Include(l => l.Lid).AsNoTracking().FirstOrDefaultAsync(l => l.Id == existing.Id);
                    if (saved != null)
                    {
                        try
                        {
                            // update collectie op main thread
                            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var idx = Uitleningen.ToList().FindIndex(u => u.Id == saved.Id);

                                // Behoud UI override vlaggen (Te laat/Inleveren) zodat de picker/labels/iconen de keuze van de gebruiker weerspiegelen
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

                                // houd SelectedUitlening gesynchroniseerd
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