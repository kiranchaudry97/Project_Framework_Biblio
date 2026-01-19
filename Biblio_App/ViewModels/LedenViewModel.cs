using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Biblio_App.Models;
using Biblio_App.Services;
using System.Collections.Generic;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Diagnostics;
using System.Resources;
using System.Globalization;
using System.Reflection;
using Biblio_Models.Resources;
using Biblio_App.Services;

namespace Biblio_App.ViewModels
{
    public partial class LedenViewModel : ObservableValidator, Biblio_App.Services.ILocalizable
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<LocalDbContext> _dbFactory;
        private readonly ILedenService? _ledenService;
        private readonly ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;
        private bool _resourceManagerInitialized = false;

        public ObservableCollection<Lid> Leden { get; } = new ObservableCollection<Lid>();

        [ObservableProperty]
        private Lid? selectedLid;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        [StringLength(100, ErrorMessageResourceName = "StringLength", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private string voornaam = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        [StringLength(100, ErrorMessageResourceName = "StringLength", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private string achternaam = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        [EmailAddress(ErrorMessage = "Ongeldig e-mailadres.")]
        [StringLength(256, ErrorMessageResourceName = "StringLength", ErrorMessageResourceType = typeof(Biblio_Models.Resources.SharedModelResource))]
        private string email = string.Empty;

        [ObservableProperty]
        [Phone(ErrorMessage = "Ongeldig telefoonnummer.")]
        private string? telefoon;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        // Foutmeldingen per veld
        public string VoornaamError => GetFirstError(nameof(Voornaam));
        public string AchternaamError => GetFirstError(nameof(Achternaam));
        public string EmailError => GetFirstError(nameof(Email));
        public string TelefoonError => GetFirstError(nameof(Telefoon));

        public bool HasValidationErrors => !string.IsNullOrEmpty(ValidationMessage) || HasErrors;

        public IRelayCommand NieuwCommand { get; }
        public IAsyncRelayCommand OpslaanCommand { get; }
        public IAsyncRelayCommand VerwijderCommand { get; }

        // Commands op item-niveau
        public IRelayCommand<Lid> ItemDetailsCommand { get; }
        public IRelayCommand<Lid> ItemEditCommand { get; }
        public IAsyncRelayCommand<Lid> ItemDeleteCommand { get; }

        public IRelayCommand ZoekCommand { get; }

        // Gelokaliseerde UI-strings
        [ObservableProperty]
        private string pageTitle = string.Empty;
        [ObservableProperty]
        private string searchPlaceholder = string.Empty;
        [ObservableProperty]
        private string searchButtonText = string.Empty;
        [ObservableProperty]
        private string newButtonText = string.Empty;
        [ObservableProperty]
        private string detailsButtonText = string.Empty;
        [ObservableProperty]
        private string editButtonText = string.Empty;
        [ObservableProperty]
        private string deleteButtonText = string.Empty;
        [ObservableProperty]
        private string saveButtonText = string.Empty;

        [ObservableProperty]
        private string overviewText = string.Empty;

        [ObservableProperty]
        private string firstNamePlaceholder = string.Empty;
        [ObservableProperty]
        private string lastNamePlaceholder = string.Empty;
        [ObservableProperty]
        private string emailPlaceholder = string.Empty;
        [ObservableProperty]
        private string phonePlaceholder = string.Empty;

        public LedenViewModel(IDbContextFactory<LocalDbContext> dbFactory, IGegevensProvider? gegevensProvider = null, ILanguageService? languageService = null, ILedenService? ledenService = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _gegevensProvider = gegevensProvider;
            _languageService = languageService;
            _ledenService = ledenService;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            ItemDetailsCommand = new AsyncRelayCommand<Lid>(async l => await NavigateToDetailsAsync(l));
            ItemEditCommand = new AsyncRelayCommand<Lid>(async l => await NavigateToEditAsync(l));
            ItemDeleteCommand = new AsyncRelayCommand<Lid>(async l => await DeleteItemAsync(l));

            ZoekCommand = new RelayCommand(async () => await LoadLedenAsync());

            // Initialiseer gelokaliseerde strings
            UpdateLocalizedStrings();
            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += (s, c) => Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
                }
            }
            catch { }

            // LAAD GEEN data direct in de constructor; dit kan leiden tot blokkering van de UI-start en ANR's.
            // Roep InitializeAsync aan vanuit de pagina's OnAppearing zodat laden na constructie en asynchroon op de UI-thread plaatsvindt.
        }

        private bool _initialized = false;

        // Publieke initializer die vanaf de pagina (OnAppearing) moet worden aangeroepen om UI-blokkering te voorkomen
        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await LoadLedenAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void EnsureResourceManagerInitialized()
        {
            if (_resourceManagerInitialized) return;
            _resourceManagerInitialized = true;
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
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
                            var test = rm.GetString("Members", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test)) { _sharedResourceManager = rm; return; }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                var modelType = typeof(SharedModelResource);
                _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", modelType.Assembly);
            }
            catch { }
        }

        private string Localize(string key)
        {
            // Geef de voorkeur aan AppShell-lokalisatie wanneer beschikbaar zodat Shell en pagina's dezelfde resource-lookup gebruiken
            try
            {
                var shell = AppShell.Instance;
                if (shell != null)
                {
                    try
                    {
                        var fromShell = shell.Translate(key);
                        if (!string.IsNullOrEmpty(fromShell)) return fromShell;
                    }
                    catch { }
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
                    "SearchPlaceholder" => "Search...",
                    "Search" => "Search",
                    "Overview" => "Overview",
                    "New" => "New",
                    "Details" => "Details",
                    "Edit" => "Edit",
                    "Delete" => "Delete",
                    "Save" => "Save",
                    "FirstName" => "First name",
                    "LastName" => "Last name",
                    "Email" => "Email",
                    "Phone" => "Phone",
                    "Ready" => "Ready",
                    "SavedMember" => "Member saved.",
                    "DeletedMember" => "Member deleted.",
                    "Error" => "Error",
                    "ErrorSavingMember" => "Error saving member.",
                    "ErrorDeletingMember" => "Error deleting member.",
                    _ => key
                };
            }

            if (code == "fr")
            {
                return key switch
                {
                    "Members" => "Membres",
                    "SearchPlaceholder" => "Rechercher...",
                    "Search" => "Rechercher",
                    "Details" => "Détails",
                    "Edit" => "Modifier",
                    "Delete" => "Supprimer",
                    "Ready" => "Terminé",
                    "SavedMember" => "Membre enregistré.",
                    "DeletedMember" => "Membre supprimé.",
                    "Error" => "Erreur",
                    "ErrorSavingMember" => "Erreur lors de l'enregistrement du membre.",
                    "ErrorDeletingMember" => "Erreur lors de la suppression du membre.",
                    _ => key
                };
            }

            return key switch
            {
                "Members" => "Leden",
                "SearchPlaceholder" => "Zoeken...",
                "Search" => "Zoek",
                "Overview" => "Overzicht",
                "New" => "Nieuw",
                "Details" => "Details",
                "Edit" => "Bewerk",
                "Delete" => "Verwijder",
                "Ready" => "Gereed",
                "SavedMember" => "Lid opgeslagen.",
                "DeletedMember" => "Lid verwijderd.",
                "Error" => "Fout",
                "ErrorSavingMember" => "Fout bij opslaan lid.",
                "ErrorDeletingMember" => "Fout bij verwijderen lid.",
                "Save" => "Opslaan",
                "FirstName" => "Voornaam",
                "LastName" => "Achternaam",
                "Email" => "Email",
                "Phone" => "Telefoon",
                _ => key
            };
        }

        public void UpdateLocalizedStrings()
        {
            UpdateLocalizedStringsCore();
        }

        private void UpdateLocalizedStringsCore()
        {
            PageTitle = Localize("Members");
            SearchPlaceholder = Localize("SearchPlaceholder");
            SearchButtonText = Localize("Search");
            // Gelokaliseerde tekst voor de Nieuw-knop
            NewButtonText = Localize("New");
            DetailsButtonText = Localize("Details");
            EditButtonText = Localize("Edit");
            DeleteButtonText = Localize("Delete");
            SaveButtonText = Localize("Save");
            OverviewText = Localize("Overview");
            FirstNamePlaceholder = Localize("FirstName");
            LastNamePlaceholder = Localize("LastName");
            EmailPlaceholder = Localize("Email");
            PhonePlaceholder = Localize("Phone");

            // Vernieuw afgeleide eigenschappen
            OnPropertyChanged(nameof(PageTitle));
        }

        // Details navigation removed - using inline editing instead
        private async Task NavigateToDetailsAsync(Lid? l)
        {
            // Details page removed - edit inline instead
            if (l == null) return;
            await ShowAlertAsync("Info", "Selecteer een lid om te bewerken.");
        }

        private async Task NavigateToEditAsync(Lid? l)
        {
            // Details page removed - edit inline instead
            if (l == null) return;
            await ShowAlertAsync("Info", "Gebruik de bewerkknop in het overzicht.");
        }

        partial void OnSelectedLidChanged(Lid? value)
        {
            // Wanneer selectie verandert, vul dan de bewerkvelden
            if (value != null)
            {
                Voornaam = value.Voornaam;
                Achternaam = value.AchterNaam;
                Email = value.Email;
                Telefoon = value.Telefoon;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
            else
            {
                Voornaam = string.Empty;
                Achternaam = string.Empty;
                Email = string.Empty;
                Telefoon = null;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
        }

        private async Task LoadLedenAsync()
        {
            try
            {
                List<Lid> list;

                // Probeer eerst API (indien internet en service beschikbaar). Val terug op lokale DB bij fout.
                var online = Connectivity.Current?.NetworkAccess == NetworkAccess.Internet;
                var loadedFromApi = false;
                list = new List<Lid>();
                if (online && _ledenService != null)
                {
                    try
                    {
                        var apiList = await _ledenService.GetLedenAsync();
                        if (apiList != null && apiList.Count > 0)
                        {
                            list = apiList;
                            loadedFromApi = true;
                        }
                        // Optioneel: filteren client-side op SearchText
                        if (loadedFromApi && !string.IsNullOrWhiteSpace(SearchText))
                        {
                            var s = SearchText.Trim();
                            list = list.Where(l => (l.Voornaam ?? string.Empty).Contains(s) || (l.AchterNaam ?? string.Empty).Contains(s) || (l.Email ?? string.Empty).Contains(s))
                                       .OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToList();
                        }
                        // Optioneel: cache naar lokale DB (best-effort)
                        if (loadedFromApi)
                        {
                            try
                            {
                                using var dbCache = _dbFactory.CreateDbContext();
                                foreach (var m in list)
                                {
                                    var existing = await dbCache.Leden.FirstOrDefaultAsync(x => x.Id == m.Id);
                                    if (existing != null)
                                    {
                                        existing.Voornaam = m.Voornaam;
                                        existing.AchterNaam = m.AchterNaam;
                                        existing.Email = m.Email;
                                        existing.Telefoon = m.Telefoon;
                                        existing.IsDeleted = false;
                                    }
                                    else
                                    {
                                        dbCache.Leden.Add(m);
                                    }
                                }
                                await dbCache.SaveChangesAsync();
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        // API faalde (bijv. 401 of backend down). Val terug op lokale database.
                        System.Diagnostics.Debug.WriteLine($"LedenViewModel: API load failed, falling back to local DB. Error: {ex}");
                        loadedFromApi = false;
                    }
                }

                if (!loadedFromApi)
                {
                    using var db = _dbFactory.CreateDbContext();
                    var q = db.Leden.AsNoTracking().Where(l => !l.IsDeleted).AsQueryable();
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var s = SearchText.Trim();
                        q = q.Where(l => (l.Voornaam ?? string.Empty).Contains(s) || (l.AchterNaam ?? string.Empty).Contains(s) || (l.Email ?? string.Empty).Contains(s));
                    }
                    list = await q.OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
                }
                
                // Update UI collection on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Leden.Clear();
                    foreach (var l in list) Leden.Add(l);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void Nieuw()
        {
            SelectedLid = null;
            Voornaam = string.Empty;
            Achternaam = string.Empty;
            Email = string.Empty;
            Telefoon = null;
            ValidationMessage = string.Empty;
            ClearErrors();
            RaiseFieldErrorProperties();
        }

        private void BuildValidationMessage()
        {
            var props = new[] { nameof(Voornaam), nameof(Achternaam), nameof(Email), nameof(Telefoon) };
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
            OnPropertyChanged(nameof(VoornaamError));
            OnPropertyChanged(nameof(AchternaamError));
            OnPropertyChanged(nameof(EmailError));
            OnPropertyChanged(nameof(TelefoonError));
            OnPropertyChanged(nameof(HasValidationErrors));
        }

        private async Task OpslaanAsync()
        {
            Debug.WriteLine("OpslaanAsync called for LedenViewModel");
            // Valideer eigenschappen met DataAnnotations op het viewmodel
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors)
            {
                BuildValidationMessage();
                await ShowAlertAsync("Validatie", ValidationMessage);
                return;
            }

            try
            {
                using var db = _dbFactory.CreateDbContext();
                if (SelectedLid == null)
                {
                    var nieuw = new Lid
                    {
                        Voornaam = Voornaam,
                        AchterNaam = Achternaam,
                        Email = Email,
                        Telefoon = Telefoon
                    };

                    db.Leden.Add(nieuw);
                }
                else
                {
                    var existing = await db.Leden.FindAsync(SelectedLid.Id);
                    if (existing != null)
                    {
                        existing.Voornaam = Voornaam;
                        existing.AchterNaam = Achternaam;
                        existing.Email = Email;
                        existing.Telefoon = Telefoon;
                        db.Leden.Update(existing);
                    }
                }

                await db.SaveChangesAsync();
                await LoadLedenAsync();

                // Informeer andere pagina's/viewmodels dat leden zijn gewijzigd
                try { Microsoft.Maui.Controls.MessagingCenter.Send(this, "MembersChanged"); } catch { }

                SelectedLid = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync(Localize("Ready"), Localize("SavedMember"));
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij opslaan.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync(Localize("Error"), Localize("ErrorSavingMember"));
            }
        }

        private async Task VerwijderAsync()
        {
            ValidationMessage = string.Empty;
            if (SelectedLid == null) return;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leden.FindAsync(SelectedLid.Id);
                if (existing != null)
                {
                    db.Leden.Remove(existing);
                    await db.SaveChangesAsync();
                }

                await LoadLedenAsync();

                // Informeer andere pagina's/viewmodels dat leden zijn gewijzigd
                try { Microsoft.Maui.Controls.MessagingCenter.Send(this, "MembersChanged"); } catch { }

                SelectedLid = null;
                await ShowAlertAsync(Localize("Ready"), Localize("DeletedMember"));
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij verwerken.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync(Localize("Error"), Localize("ErrorDeletingMember"));
            }
        }

        private async Task DeleteItemAsync(Lid? item)
        {
            if (item == null) return;

            try
            {
                Debug.WriteLine($"DeleteItemAsync called for Lid Id={item.Id}");
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leden.FindAsync(item.Id);
                if (existing != null)
                {
                    db.Leden.Remove(existing);
                    await db.SaveChangesAsync();
                }

                await LoadLedenAsync();

                // Informeer andere pagina's/viewmodels dat leden zijn gewijzigd
                try { Microsoft.Maui.Controls.MessagingCenter.Send(this, "MembersChanged"); } catch { }

                await ShowAlertAsync(Localize("Ready"), Localize("DeletedMember"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync(Localize("Error"), Localize("ErrorDeletingMember"));
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
            catch
            {
                // negeren
            }
        }
    }
}