using System;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Biblio_App.Services;
using Biblio_Models.Entiteiten;

namespace Biblio_App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly Action? _openBoekenAction;
        private readonly Action? _openLedenAction;
        private readonly Action? _openUitleningenAction;
        private readonly Action? _openCategorieenAction;

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
        public MainViewModel() : this(null, null, null, null, null) { }

        // DI ctor used in MauiProgram
        public MainViewModel(IGegevensProvider? gegevensProvider = null,
                             Action? openBoeken = null,
                             Action? openLeden = null,
                             Action? openUitleningen = null,
                             Action? openCategorieen = null)
        {
            _gegevensProvider = gegevensProvider;
            _openBoekenAction = openBoeken;
            _openLedenAction = openLeden;
            _openUitleningenAction = openUitleningen;
            _openCategorieenAction = openCategorieen;

            // initialize status
            IsVerbonden = NetworkInterface.GetIsNetworkAvailable();
            SynchronisatieStatus = IsVerbonden ? "Online" : "Offline - offlinegegevens in gebruik";

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
                SynchronisatieStatus = IsVerbonden ? "Online" : "Offline - offlinegegevens in gebruik";

                if (_gegevensProvider != null && IsVerbonden)
                {
                    try
                    {
                        var result = await _gegevensProvider.GetTellersAsync();
                        TotaalBoeken = result.boeken;
                        TotaalLeden = result.leden;
                        OpenUitleningen = result.openUitleningen;
                        SynchronisatieStatus = "Online";
                    }
                    catch (Exception ex)
                    {
                        SynchronisatieStatus = $"Online (fout bij ophalen gegevens): {ex.Message}";
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
    }
}
