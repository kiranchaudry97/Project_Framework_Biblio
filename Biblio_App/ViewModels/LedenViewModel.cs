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
using Biblio_App.Models;
using Biblio_App.Services;
using System.Collections.Generic;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Biblio_App.ViewModels
{
    public partial class LedenViewModel : ObservableValidator
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;

        public ObservableCollection<Lid> Leden { get; } = new ObservableCollection<Lid>();

        [ObservableProperty]
        private Lid? selectedLid;

        [ObservableProperty]
        [Required(ErrorMessage = "Voornaam is verplicht.")]
        [StringLength(100, ErrorMessage = "Voornaam is te lang.")]
        private string voornaam = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Achternaam is verplicht.")]
        [StringLength(100, ErrorMessage = "Achternaam is te lang.")]
        private string achternaam = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Email is verplicht.")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mailadres.")]
        [StringLength(256, ErrorMessage = "Email is te lang.")]
        private string email = string.Empty;

        [ObservableProperty]
        [Phone(ErrorMessage = "Ongeldig telefoonnummer.")]
        private string? telefoon;

        [ObservableProperty]
        [StringLength(300, ErrorMessage = "Adres is te lang.")]
        private string? adres;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        // Per-field error properties
        public string VoornaamError => GetFirstError(nameof(Voornaam));
        public string AchternaamError => GetFirstError(nameof(Achternaam));
        public string EmailError => GetFirstError(nameof(Email));
        public string TelefoonError => GetFirstError(nameof(Telefoon));
        public string AdresError => GetFirstError(nameof(Adres));

        public bool HasValidationErrors => !string.IsNullOrEmpty(ValidationMessage) || HasErrors;

        public IRelayCommand NieuwCommand { get; }
        public IAsyncRelayCommand OpslaanCommand { get; }
        public IAsyncRelayCommand VerwijderCommand { get; }

        // item-level commands
        public IRelayCommand<Lid> ItemDetailsCommand { get; }
        public IRelayCommand<Lid> ItemEditCommand { get; }
        public IAsyncRelayCommand<Lid> ItemDeleteCommand { get; }

        public IRelayCommand ZoekCommand { get; }

        public LedenViewModel(IDbContextFactory<BiblioDbContext> dbFactory, IGegevensProvider? gegevensProvider = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _gegevensProvider = gegevensProvider;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            ItemDetailsCommand = new AsyncRelayCommand<Lid>(async l => await NavigateToDetailsAsync(l));
            ItemEditCommand = new RelayCommand<Lid>(l => { if (l != null) SelectedLid = l; });
            ItemDeleteCommand = new AsyncRelayCommand<Lid>(async l => await DeleteItemAsync(l));

            ZoekCommand = new RelayCommand(async () => await LoadLedenAsync());

            _ = LoadLedenAsync();
        }

        private async Task NavigateToDetailsAsync(Lid? l)
        {
            if (l == null) return;
            try
            {
                await Shell.Current.GoToAsync($"{nameof(Pages.LidDetailsPage)}?lidId={l.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", "Kan detailspagina niet openen.");
            }
        }

        partial void OnSelectedLidChanged(Lid? value)
        {
            // When selection changes, populate editable fields
            if (value != null)
            {
                Voornaam = value.Voornaam;
                Achternaam = value.AchterNaam;
                Email = value.Email;
                Telefoon = value.Telefoon;
                Adres = value.Adres;
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
                Adres = null;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
        }

        private async Task LoadLedenAsync()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var q = db.Leden.AsNoTracking().Where(l => !l.IsDeleted).AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var s = SearchText.Trim();
                    q = q.Where(l => (l.Voornaam ?? string.Empty).Contains(s) || (l.AchterNaam ?? string.Empty).Contains(s) || (l.Email ?? string.Empty).Contains(s));
                }
                var list = await q.OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
                Leden.Clear();
                foreach (var l in list) Leden.Add(l);
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
            Adres = null;
            ValidationMessage = string.Empty;
            ClearErrors();
            RaiseFieldErrorProperties();
        }

        private void BuildValidationMessage()
        {
            var props = new[] { nameof(Voornaam), nameof(Achternaam), nameof(Email), nameof(Telefoon), nameof(Adres) };
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
            OnPropertyChanged(nameof(AdresError));
            OnPropertyChanged(nameof(HasValidationErrors));
        }

        private async Task OpslaanAsync()
        {
            // validate properties using DataAnnotations on VM
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
                        Telefoon = Telefoon,
                        Adres = Adres
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
                        existing.Adres = Adres;
                        db.Leden.Update(existing);
                    }
                }

                await db.SaveChangesAsync();
                await LoadLedenAsync();

                SelectedLid = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Lid opgeslagen.");
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij opslaan.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
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
                SelectedLid = null;
                await ShowAlertAsync("Gereed", "Lid verwijderd.");
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij verwijderen.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
            }
        }

        private async Task DeleteItemAsync(Lid? item)
        {
            if (item == null) return;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leden.FindAsync(item.Id);
                if (existing != null)
                {
                    db.Leden.Remove(existing);
                    await db.SaveChangesAsync();
                }

                await LoadLedenAsync();
                await ShowAlertAsync("Gereed", "Lid verwijderd.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", "Onverwachte fout bij verwijderen.");
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
                // ignore
            }
        }
    }
}