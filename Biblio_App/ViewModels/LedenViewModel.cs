using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using Biblio_App.Services;
using System.Threading.Tasks;
using System;
using System.Linq;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Biblio_App.ViewModels
{
    public partial class LedenViewModel : ObservableValidator
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly BiblioDbContext _db;

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

        public LedenViewModel(BiblioDbContext db, IGegevensProvider? gegevensProvider = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _gegevensProvider = gegevensProvider;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            LoadLeden();
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

        private void LoadLeden()
        {
            var list = _db.Leden.AsNoTracking().ToList();
            Leden.Clear();
            foreach (var l in list) Leden.Add(l);
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
            // aggregate errors from properties
            var props = new[] { nameof(Voornaam), nameof(Achternaam), nameof(Email), nameof(Telefoon), nameof(Adres) };
            var messages = new List<string>();
            var notifier = (System.ComponentModel.INotifyDataErrorInfo)this;
            foreach (var p in props)
            {
                var errors = notifier.GetErrors(p) as IEnumerable;
                if (errors != null)
                {
                    foreach (var e in errors)
                    {
                        messages.Add(e?.ToString());
                    }
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
                foreach (var e in errors)
                {
                    if (e != null) return e.ToString();
                }
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

            // unique email check
            var selectedId = SelectedLid != null ? SelectedLid.Id : 0;
            var exists = _db.Leden.Any(l => l.Email == Email && l.Id != selectedId);
            if (exists)
            {
                ValidationMessage = "Email bestaat al in de database.";
                await ShowAlertAsync("Validatie", ValidationMessage);
                return;
            }

            try
            {
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
                    _db.Leden.Add(nieuw);
                }
                else
                {
                    var existing = await _db.Leden.FindAsync(SelectedLid.Id);
                    if (existing != null)
                    {
                        existing.Voornaam = Voornaam;
                        existing.AchterNaam = Achternaam;
                        existing.Email = Email;
                        existing.Telefoon = Telefoon;
                        existing.Adres = Adres;
                        _db.Leden.Update(existing);
                    }
                }

                await _db.SaveChangesAsync();
                LoadLeden();
                SelectedLid = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Lid opgeslagen.");
            }
            catch (DbUpdateException ex)
            {
                ValidationMessage = "Fout bij opslaan. Controleer ingevoerde waarden en probeer opnieuw.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
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
            var existing = await _db.Leden.FindAsync(SelectedLid.Id);
            if (existing != null)
            {
                try
                {
                    _db.Leden.Remove(existing);
                    await _db.SaveChangesAsync();
                    LoadLeden();
                    SelectedLid = null;
                    await ShowAlertAsync("Gereed", "Lid verwijderd.");
                }
                catch (DbUpdateException ex)
                {
                    ValidationMessage = "Fout bij verwijderen. Dit lid kan gekoppeld zijn aan uitleningen of andere records.";
                    System.Diagnostics.Debug.WriteLine(ex);
                    await ShowAlertAsync("Fout", ValidationMessage);
                }
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