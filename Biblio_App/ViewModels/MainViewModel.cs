using System;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Biblio_App.Services;
using Biblio_Models.Entiteiten;
using Biblio_App.Models;
using System.Globalization;
using System.Resources;
using System.Linq;
using System.Reflection;
using Biblio_Models.Resources;

namespace Biblio_App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Dit ViewModel is de "home/dashboard" van de app.
        // Het toont tellers (aantal boeken/leden/uitleningen) en heeft knoppen
        // om te navigeren + een knop om alles te synchroniseren.

        // Data-provider: abstractie om tellers op te halen (meestal uit lokale DB)
        private readonly IGegevensProvider? _gegevensProvider;

        // DataSyncService: kan alle data ophalen van de API en lokaal opslaan
        private readonly IDataSyncService? _dataSync;

        // LanguageService: huidige taal/cultuur (voor vertaling van UI strings)
        private readonly ILanguageService? _languageService;

        // Navigatie-acties worden via DI als lambda callbacks doorgegeven.
        // Zo blijft de ViewModel onafhankelijk van Shell/UI.
        private readonly Func<Task>? _openBoeken;
        private readonly Func<Task>? _openLeden;
        private readonly Func<Task>? _openUitleningen;
        private readonly Func<Task>? _openCategorieen;

        private ResourceManager? _sharedResourceManager;
        private bool _resourceManagerInitialized = false;

        [ObservableProperty]
        private int totaalBoeken;

        [ObservableProperty]
        private int totaalLeden;

        [ObservableProperty]
        private int openUitleningen;

        [ObservableProperty]
        private bool isVerbonden;

        [ObservableProperty]
        private string synchronisatieStatus = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        // Convenience property for XAML to avoid converters
        [ObservableProperty]
        private bool isNotBusy = true;

        partial void OnIsBusyChanged(bool value)
        {
            IsNotBusy = !value;
        }

        // Commands die door MainPage.xaml worden gebruikt (MVVM)
        public IAsyncRelayCommand NavigateToBoekenCommand { get; }
        public IAsyncRelayCommand NavigateToLedenCommand { get; }
        public IAsyncRelayCommand NavigateToUitleningenCommand { get; }
        public IAsyncRelayCommand NavigateToCategorieenCommand { get; }

        public IAsyncRelayCommand VernieuwenAsyncCommand { get; }
        public IAsyncRelayCommand SyncAsyncCommand { get; }

        // Use source-generated command names via [RelayCommand]

        // Parameterless ctor for tooling
        public MainViewModel() : this(
            gegevensProvider: null,
            dataSync: null,
            languageService: null,
            openBoeken: null,
            openLeden: null,
            openUitleningen: null,
            openCategorieen: null)
        { }

        // DI ctor - simplified
        public MainViewModel(
            IGegevensProvider? gegevensProvider = null,
            IDataSyncService? dataSync = null,
            ILanguageService? languageService = null,
            Func<Task>? openBoeken = null,
            Func<Task>? openLeden = null,
            Func<Task>? openUitleningen = null,
            Func<Task>? openCategorieen = null)
        {
            // Dependencies opslaan zodat we ze later kunnen gebruiken
            _gegevensProvider = gegevensProvider;
            _dataSync = dataSync;
            _languageService = languageService;
            _openBoeken = openBoeken;
            _openLeden = openLeden;
            _openUitleningen = openUitleningen;
            _openCategorieen = openCategorieen;

            // Navigatie commands: als er geen lambda werd meegegeven, doen we gewoon niets.
            NavigateToBoekenCommand = new AsyncRelayCommand(async () =>
            {
                if (_openBoeken != null) await _openBoeken();
            });
            NavigateToLedenCommand = new AsyncRelayCommand(async () =>
            {
                if (_openLeden != null) await _openLeden();
            });
            NavigateToUitleningenCommand = new AsyncRelayCommand(async () =>
            {
                if (_openUitleningen != null) await _openUitleningen();
            });
            NavigateToCategorieenCommand = new AsyncRelayCommand(async () =>
            {
                if (_openCategorieen != null) await _openCategorieen();
            });

            VernieuwenAsyncCommand = new AsyncRelayCommand(VernieuwenAsync);
            SyncAsyncCommand = new AsyncRelayCommand(SyncAsync);

            // No manual sync command; rely on generated SyncAsyncCommand

            // Status initialiseren: toon online/offline bij opstart
            IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
            SynchronisatieStatus = IsVerbonden ? "Online" : "Offline";

            // Als de taal wijzigt, willen we bepaalde labels opnieuw vertalen
            if (_languageService != null)
            {
                _languageService.LanguageChanged += (s, c) =>
                {
                    SynchronisatieStatus = IsVerbonden ? Localize("Online") : Localize("Offline");
                };
            }
        }

        [RelayCommand]
        public async Task VernieuwenAsync()
        {
            // IsBusy patroon: voorkomt dubbel klikken / meerdere gelijktijdige loads
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // 1) Check netwerkstatus voor UI feedback
                IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
                SynchronisatieStatus = IsVerbonden ? Localize("Online") : Localize("Offline");

                if (_gegevensProvider != null)
                {
                    try
                    {
                        // 2) Haal tellers op (meestal uit lokale DB)
                        var result = await _gegevensProvider.GetTellersAsync();
                        TotaalBoeken = result.boeken;
                        TotaalLeden = result.leden;
                        OpenUitleningen = result.openUitleningen;
                    }
                    catch (Exception ex)
                    {
                        // try/catch rond provider zodat UI niet crasht bij DB/IO fouten
                        SynchronisatieStatus = $"Error: {ex.Message}";
                    }
                }
            }
            finally
            {
                // altijd IsBusy terug uitzetten (ook bij fouten)
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SyncAsync()
        {
            // Sync is optioneel: als er geen DataSyncService is, doen we niks.
            if (IsBusy || _dataSync == null) return;
            
            IsBusy = true;
            try
            {
                // 1) UI status tonen
                SynchronisatieStatus = "Syncing...";

                // 2) Alle datasets synchroniseren (API -> lokaal)
                await _dataSync.SyncAllAsync();

                // 3) UI status updaten
                SynchronisatieStatus = "Sync complete";

                // 4) Tellers opnieuw ophalen na sync
                if (_gegevensProvider != null)
                {
                    var result = await _gegevensProvider.GetTellersAsync();
                    TotaalBoeken = result.boeken;
                    TotaalLeden = result.leden;
                    OpenUitleningen = result.openUitleningen;
                }
            }
            catch (Exception ex)
            {
                // Bij fout tonen we een boodschap. De app blijft werken (offline-first).
                SynchronisatieStatus = $"Sync error: {ex.Message}";
            }
            finally
            {
                // altijd IsBusy terug uitzetten
                IsBusy = false;
            }
        }

        private void EnsureResourceManagerInitialized()
        {
            if (_resourceManagerInitialized) return;
            _resourceManagerInitialized = true;

            // Try web shared resource first (Biblio_Web.Resources.Vertalingen.SharedResource)
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

            // Fallback to SharedModelResource in Biblio_Models
            try
            {
                var modelType = typeof(SharedModelResource);
                if (modelType != null)
                {
                    var asm = modelType.Assembly;
                    _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", asm);
                    return;
                }
            }
            catch { }

            // if neither found, leave null
        }

        private string Localize(string key, string? arg = null)
        {
            EnsureResourceManagerInitialized();

            var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;

            if (_sharedResourceManager != null)
            {
                try
                {
                    var value = _sharedResourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(value))
                    {
                        return arg != null ? string.Format(culture, value, arg) : value;
                    }
                }
                catch { }
            }

            // Fallback inline translations
            var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            return key switch
            {
                "Online" => code == "en" ? "Online" : "Online",
                "Offline" => code == "en" ? "Offline - offline data in use" : "Offline - offlinegegevens in gebruik",
                "Syncing" => code == "en" ? "Synchronizing..." : "Synchroniseren...",
                "NoSyncService" => code == "en" ? "No sync service available." : "Geen sync-service beschikbaar.",
                "SyncComplete" => code == "en" ? "Synchronization complete." : "Synchronisatie voltooid.",
                "SyncError" => code == "en" ? $"Error during synchronization: {arg}" : $"Fout bij synchronisatie: {arg}",
                "OnlineWithError" => code == "en" ? $"Online (error fetching data): {arg}" : $"Online (fout bij ophalen gegevens): {arg}",
                _ => key
            };
        }
    }
}
