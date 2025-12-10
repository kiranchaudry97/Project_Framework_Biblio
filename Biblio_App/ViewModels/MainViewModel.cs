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
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly Action? _openBoekenAction;
        private readonly Action? _openLedenAction;
        private readonly Action? _openUitleningenAction;
        private readonly Action? _openCategorieenAction;
        private readonly IDataSyncService? _dataSync;
        private readonly ILanguageService? _languageService;

        private ResourceManager? _sharedResourceManager;
        private bool _resourceManagerInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Boek> boeken = new ObservableCollection<Boek>();

        [ObservableProperty]
        private Boek? selectedBoek;

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

        // Parameterless ctor for tooling
        public MainViewModel() : this(null, null, null, null, null, null, null) { }

        // DI ctor used in MauiProgram
        public MainViewModel(IGegevensProvider? gegevensProvider = null,
                             Action? openBoeken = null,
                             Action? openLeden = null,
                             Action? openUitleningen = null,
                             Action? openCategorieen = null,
                             IDataSyncService? dataSync = null,
                             ILanguageService? languageService = null)
        {
            _gegevensProvider = gegevensProvider;
            _openBoekenAction = openBoeken;
            _openLedenAction = openLeden;
            _openUitleningenAction = openUitleningen;
            _openCategorieenAction = openCategorieen;
            _dataSync = dataSync;
            _languageService = languageService;

            // initialize status
            IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
            SynchronisatieStatus = IsVerbonden ? Localize("Online") : Localize("Offline");

            // subscribe to language changes to update localized strings
            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += (s, c) =>
                    {
                        // update status message when language changes
                        SynchronisatieStatus = IsVerbonden ? Localize("Online") : Localize("Offline");
                    };
                }
            }
            catch { }

            // sample data for design/runtime when DB/provider not available
            if (Boeken.Count == 0)
            {
                Boeken.Add(new Boek { Titel = "Voorbeeldboek 1", Auteur = "A. Auteur" });
                Boeken.Add(new Boek { Titel = "Voorbeeldboek 2", Auteur = "B. Schrijver" });
                TotaalBoeken = Boeken.Count;
            }
        }

        [RelayCommand]
        public async Task VernieuwenAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
                SynchronisatieStatus = IsVerbonden ? Localize("Online") : Localize("Offline");

                if (_gegevensProvider != null && IsVerbonden)
                {
                    try
                    {
                        var result = await _gegevensProvider.GetTellersAsync();
                        TotaalBoeken = result.boeken;
                        TotaalLeden = result.leden;
                        OpenUitleningen = result.openUitleningen;
                        SynchronisatieStatus = Localize("Online");
                    }
                    catch (Exception ex)
                    {
                        SynchronisatieStatus = Localize("OnlineWithError", ex.Message);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void VoegToe()
        {
            var nieuw = new Boek { Titel = "Nieuw boek", Auteur = "Onbekend" };
            Boeken.Add(nieuw);
            SelectedBoek = nieuw;
            TotaalBoeken = Boeken.Count;
        }

        // Rename navigation commands to avoid name conflicts with properties
        [RelayCommand]
        public void NavigateToBoeken() => _openBoekenAction?.Invoke();

        [RelayCommand]
        public void NavigateToLeden() => _openLedenAction?.Invoke();

        [RelayCommand]
        public void NavigateToUitleningen() => _openUitleningenAction?.Invoke();

        [RelayCommand]
        public void NavigateToCategorieen() => _openCategorieenAction?.Invoke();

        // Sync command to pull remote data and store locally
        [RelayCommand]
        public async Task SyncAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                SynchronisatieStatus = Localize("Syncing");
                if (_dataSync == null)
                {
                    SynchronisatieStatus = Localize("NoSyncService");
                    return;
                }

                await _dataSync.SyncAllAsync();
                SynchronisatieStatus = Localize("SyncComplete");

                // Optionally refresh counts after sync
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
                SynchronisatieStatus = Localize("SyncError", ex.Message);
            }
            finally
            {
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
