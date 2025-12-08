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

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel : ObservableValidator
    {
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;

        public ObservableCollection<Lenen> Uitleningen { get; } = new ObservableCollection<Lenen>();
        public ObservableCollection<Boek> BoekenList { get; } = new ObservableCollection<Boek>();
        public ObservableCollection<Lid> LedenList { get; } = new ObservableCollection<Lid>();
        public ObservableCollection<Categorie> Categorieen { get; } = new ObservableCollection<Categorie>();

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

        // filter/search
        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private Categorie? selectedCategory;

        [ObservableProperty]
        private bool onlyOpen;

        [ObservableProperty]
        private string lastError = string.Empty;

        public int LedenCount => LedenList?.Count ?? 0;
        public int UitleningenCount => Uitleningen?.Count ?? 0;
        public int BoekenCount => BoekenList?.Count ?? 0;

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
        public IAsyncRelayCommand<Lenen?> ReturnCommand { get; }
        public IAsyncRelayCommand<Lenen?> DeleteCommand { get; }

        public UitleningenViewModel(IDbContextFactory<BiblioDbContext> dbFactory)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            ZoekCommand = new RelayCommand(async () => await LoadDataWithFiltersAsync());
            ReturnCommand = new AsyncRelayCommand<Lenen?>(ReturnAsync);
            DeleteCommand = new AsyncRelayCommand<Lenen?>(DeleteAsync);

            _ = LoadDataAsync();
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

        private void RaiseCountProperties()
        {
            OnPropertyChanged(nameof(LedenCount));
            OnPropertyChanged(nameof(UitleningenCount));
            OnPropertyChanged(nameof(BoekenCount));
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

        // Public wrapper so page can explicitly request a reload when appearing
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
                    query = query.Where(l => (l.Boek != null && (l.Boek.Titel ?? string.Empty).ToLower().Contains(s))
                        || (l.Boek != null && (l.Boek.Auteur ?? string.Empty).ToLower().Contains(s))
                        || (l.Lid != null && ((l.Lid.Voornaam ?? string.Empty) + " " + (l.Lid.AchterNaam ?? string.Empty)).ToLower().Contains(s))
                        || (l.Lid != null && (l.Lid.Email ?? string.Empty).ToLower().Contains(s))
                    );
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
                await ShowAlertAsync("Gereed", "Uitlening verwijderd.");
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Kan uitlening niet verwijderen; mogelijk gekoppeld.";
                await ShowAlertAsync("Fout", ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Onverwachte fout bij verwijderen uitlening.";
                await ShowAlertAsync("Fout", ValidationMessage);
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
                await ShowAlertAsync("Gereed", "Uitlening verwijderd.");
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Kan uitlening niet verwijderen; mogelijk gekoppeld.";
                await ShowAlertAsync("Fout", ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Onverwachte fout bij verwijderen uitlening.";
                await ShowAlertAsync("Fout", ValidationMessage);
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
                await ShowAlertAsync("Gereed", "Boek als ingeleverd gemarkeerd.");
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Kan uitlening niet updaten.";
                await ShowAlertAsync("Fout", ValidationMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ValidationMessage = "Onverwachte fout bij updaten uitlening.";
                await ShowAlertAsync("Fout", ValidationMessage);
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