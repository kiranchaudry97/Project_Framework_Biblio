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
        private readonly IDataSyncService? _dataSync;
        private readonly ILanguageService? _languageService;

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

        // Parameterless ctor for tooling
        public MainViewModel() : this(null, null, null) { }

        // DI ctor - simplified
        public MainViewModel(
            IGegevensProvider? gegevensProvider = null,
            IDataSyncService? dataSync = null,
            ILanguageService? languageService = null)
        {
            _gegevensProvider = gegevensProvider;
            _dataSync = dataSync;
            _languageService = languageService;

            // Initialize status
            IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
            SynchronisatieStatus = IsVerbonden ? "Online" : "Offline";

            // Subscribe to language changes
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
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
                SynchronisatieStatus = IsVerbonden ? Localize("Online") : Localize("Offline");

                if (_gegevensProvider != null)
                {
                    try
                    {
                        var result = await _gegevensProvider.GetTellersAsync();
                        TotaalBoeken = result.boeken;
                        TotaalLeden = result.leden;
                        OpenUitleningen = result.openUitleningen;
                    }
                    catch (Exception ex)
                    {
                        SynchronisatieStatus = $"Error: {ex.Message}";
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SyncAsync()
        {
            if (IsBusy || _dataSync == null) return;
            
            IsBusy = true;
            try
            {
                SynchronisatieStatus = "Syncing...";
                await _dataSync.SyncAllAsync();
                SynchronisatieStatus = "Sync complete";

                // Refresh counts
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
                SynchronisatieStatus = $"Sync error: {ex.Message}";
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
