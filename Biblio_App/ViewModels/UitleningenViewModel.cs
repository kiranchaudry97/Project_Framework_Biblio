using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Windows.Input;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel : ObservableValidator
    {
        private readonly BiblioDbContext _db;

        public ObservableCollection<Lenen> Uitleningen { get; } = new ObservableCollection<Lenen>();
        public ObservableCollection<Boek> BoekenList { get; } = new ObservableCollection<Boek>();
        public ObservableCollection<Lid> LedenList { get; } = new ObservableCollection<Lid>();

        [ObservableProperty]
        private Lenen? selectedUitlening;

        // form selection objects
        [ObservableProperty]
        private Boek? selectedBoek;

        [ObservableProperty]
        private Lid? selectedLid;

        [ObservableProperty]
        [Required(ErrorMessage = "Startdatum is verplicht.")]
        private DateTime startDate = DateTime.Now;

        [ObservableProperty]
        [Required(ErrorMessage = "Einddatum is verplicht.")]
        private DateTime dueDate = DateTime.Now.AddDays(14);

        [ObservableProperty]
        private DateTime? returnedAt;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        // per-field errors (keep BoekId/LidId errors but also show object-based)
        public string BoekError => SelectedBoek == null ? "" : string.Empty; // placeholder, main errors via ValidationMessage
        public string LidError => SelectedLid == null ? "" : string.Empty;
        public string StartDateError => GetFirstError(nameof(StartDate));
        public string DueDateError => GetFirstError(nameof(DueDate));

        public bool HasValidationErrors => !string.IsNullOrEmpty(ValidationMessage) || HasErrors;

        public IRelayCommand NieuwCommand { get; }
        public IAsyncRelayCommand OpslaanCommand { get; }
        public IAsyncRelayCommand VerwijderCommand { get; }

        public UitleningenViewModel(BiblioDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            LoadData();
        }

        partial void OnSelectedUitleningChanged(Lenen? value)
        {
            if (value != null)
            {
                SelectedBoek = value.Boek;
                SelectedLid = value.Lid;
                StartDate = value.StartDate;
                DueDate = value.DueDate;
                ReturnedAt = value.ReturnedAt;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
            else
            {
                SelectedBoek = null;
                SelectedLid = null;
                StartDate = DateTime.Now;
                DueDate = DateTime.Now.AddDays(14);
                ReturnedAt = null;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
        }

        private void LoadData()
        {
            var list = _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).AsNoTracking().ToList();
            Uitleningen.Clear();
            foreach (var u in list) Uitleningen.Add(u);

            BoekenList.Clear();
            foreach (var b in _db.Boeken.AsNoTracking().ToList()) BoekenList.Add(b);

            LedenList.Clear();
            foreach (var l in _db.Leden.AsNoTracking().ToList()) LedenList.Add(l);
        }

        private void Nieuw() => SelectedUitlening = null;

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

            // object selection errors
            if (SelectedBoek == null) messages.Add("Boek is verplicht.");
            if (SelectedLid == null) messages.Add("Lid is verplicht.");

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
                await ShowAlertAsync("Validatie", ValidationMessage);
                return;
            }

            try
            {
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
                    _db.Leningens.Add(nieuw);
                }
                else
                {
                    var existing = await _db.Leningens.FindAsync(SelectedUitlening.Id);
                    if (existing != null)
                    {
                        existing.BoekId = SelectedBoek.Id;
                        existing.LidId = SelectedLid.Id;
                        existing.StartDate = StartDate;
                        existing.DueDate = DueDate;
                        existing.ReturnedAt = ReturnedAt;
                        _db.Leningens.Update(existing);
                    }
                }

                await _db.SaveChangesAsync();
                LoadData();
                SelectedUitlening = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Uitlening opgeslagen.");
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Fout bij opslaan uitlening.";
                await ShowAlertAsync("Fout", ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Onverwachte fout bij opslaan uitlening.";
                await ShowAlertAsync("Fout", ValidationMessage);
            }
        }

        private async Task VerwijderAsync()
        {
            if (SelectedUitlening == null) return;
            var existing = await _db.Leningens.FindAsync(SelectedUitlening.Id);
            if (existing != null)
            {
                try
                {
                    _db.Leningens.Remove(existing);
                    await _db.SaveChangesAsync();
                    LoadData();
                    SelectedUitlening = null;
                    await ShowAlertAsync("Gereed", "Uitlening verwijderd.");
                }
                catch (DbUpdateException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    ValidationMessage = "Kan uitlening niet verwijderen; mogelijk gekoppeld.";
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