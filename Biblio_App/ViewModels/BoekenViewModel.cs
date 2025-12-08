using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Biblio_Models.Entiteiten;
using Biblio_App.Services;
using System.ComponentModel;
using System;
using System.Linq;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biblio_App.Models.Pagination;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;

namespace Biblio_App.ViewModels
{
    public partial class BoekenViewModel : ObservableValidator
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;
        private readonly IDataSyncService? _sync;

        private CancellationTokenSource? _searchCts;

        public ObservableCollection<Boek> Boeken { get; } = new ObservableCollection<Boek>();
        public ObservableCollection<Categorie> Categorien { get; } = new ObservableCollection<Categorie>();

        [ObservableProperty]
        private Boek? selectedBoek;

        [ObservableProperty]
        [Required(ErrorMessage = "Titel is verplicht.")]
        [StringLength(200, ErrorMessage = "Titel is te lang.")]
        private string titel = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Auteur is verplicht.")]
        [StringLength(200, ErrorMessage = "Auteur is te lang.")]
        private string auteur = string.Empty;

        [ObservableProperty]
        [StringLength(17, ErrorMessage = "ISBN is te lang.")]
        private string isbn = string.Empty;

        [ObservableProperty]
        private int categorieId;

        // Filter/search properties
        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private Categorie? selectedFilterCategorie;

        [ObservableProperty]
        private Categorie? selectedCategory;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        // per-field error properties
        public string TitelError => GetFirstError(nameof(Titel));
        public string AuteurError => GetFirstError(nameof(Auteur));
        public string IsbnError => GetFirstError(nameof(Isbn));

        public bool HasValidationErrors => !string.IsNullOrEmpty(ValidationMessage) || HasErrors;

        public IRelayCommand NieuwCommand { get; }
        public IAsyncRelayCommand OpslaanCommand { get; }
        public IAsyncRelayCommand VerwijderCommand { get; }
        public IRelayCommand ZoekCommand { get; }

        // item commands
        public IRelayCommand<Boek> ItemDetailsCommand { get; }
        public IRelayCommand<Boek> ItemEditCommand { get; }
        public IAsyncRelayCommand<Boek> ItemDeleteCommand { get; }

        public BoekenViewModel(IDbContextFactory<BiblioDbContext> dbFactory, IDataSyncService? sync = null, IGegevensProvider? gegevensProvider = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _sync = sync;
            _gegevensProvider = gegevensProvider;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);
            ZoekCommand = new RelayCommand(async () => await ZoekAsync());

            ItemDetailsCommand = new RelayCommand<Boek>(async b => await NavigateToDetailsAsync(b));
            ItemEditCommand = new RelayCommand<Boek>(b => { if (b != null) SelectedBoek = b; });
            ItemDeleteCommand = new AsyncRelayCommand<Boek>(async b => await DeleteItemAsync(b));

            _ = LoadCategoriesAsync();
            _ = LoadBooksAsync();
        }

        partial void OnSelectedBoekChanged(Boek? value)
        {
            if (value != null)
            {
                Titel = value.Titel;
                Auteur = value.Auteur;
                Isbn = value.Isbn;
                CategorieId = value.CategorieID;
                SelectedCategory = Categorien.FirstOrDefault(c => c.Id == value.CategorieID);
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
            else
            {
                Titel = string.Empty;
                Auteur = string.Empty;
                Isbn = string.Empty;
                CategorieId = 0;
                SelectedCategory = null;
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
        }

        partial void OnSelectedCategoryChanged(Categorie? value)
        {
            if (value != null) CategorieId = value.Id;
        }

        private async Task LoadCategoriesAsync()
        {
            Categorien.Clear();
            Categorien.Add(new Categorie { Id = 0, Naam = "Alle" });
            // prefer sync service when available
            if (_sync != null)
            {
                try
                {
                    var cats = await _sync.GetCategorieenAsync(true);
                    foreach (var c in cats) Categorien.Add(c);
                    SelectedFilterCategorie = Categorien.FirstOrDefault();
                    return;
                }
                catch { /* fallback to local */ }
            }

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var cats = await db.Categorien.Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync();
                foreach (var c in cats) Categorien.Add(c);
            }
            catch { }
            SelectedFilterCategorie = Categorien.FirstOrDefault();
        }

        private async Task LoadBooksAsync()
        {
            // prefer sync service when available
            if (_sync != null)
            {
                try
                {
                    var list = await _sync.GetBoekenAsync(true);
                    Boeken.Clear();
                    foreach (var b in list) Boeken.Add(b);
                    return;
                }
                catch { /* fallback to local */ }
            }

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var list = await db.Boeken.Include(b => b.categorie).Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();
                Boeken.Clear();
                foreach (var b in list) Boeken.Add(b);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async Task LoadBooksFromApiWithFiltersAsync()
        {
            // prefer sync service when available
            if (_sync != null)
            {
                try
                {
                    var items = await _sync.GetBoekenAsync(true);
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var s = SearchText.Trim().ToLowerInvariant();
                        items = items.Where(b => (b.Titel ?? string.Empty).ToLower().Contains(s) || (b.Auteur ?? string.Empty).ToLower().Contains(s) || (b.Isbn ?? string.Empty).ToLower().Contains(s)).ToList();
                    }
                    if (SelectedFilterCategorie != null && SelectedFilterCategorie.Id != 0)
                    {
                        items = items.Where(b => b.CategorieID == SelectedFilterCategorie.Id).ToList();
                    }
                    Boeken.Clear();
                    foreach (var b in items) Boeken.Add(b);
                    return;
                }
                catch { /* fallback */ }
            }

            // local filtering
            using var db = _dbFactory.CreateDbContext();
            var itemsLocal = await db.Boeken.Include(b => b.categorie).Where(b => !b.IsDeleted).ToListAsync();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.Trim().ToLowerInvariant();
                itemsLocal = itemsLocal.Where(b => (b.Titel ?? string.Empty).ToLower().Contains(s) || (b.Auteur ?? string.Empty).ToLower().Contains(s) || (b.Isbn ?? string.Empty).ToLower().Contains(s)).ToList();
            }
            if (SelectedFilterCategorie != null && SelectedFilterCategorie.Id != 0)
            {
                itemsLocal = itemsLocal.Where(b => b.CategorieID == SelectedFilterCategorie.Id).ToList();
            }
            Boeken.Clear();
            foreach (var b in itemsLocal) Boeken.Add(b);
        }

        private async Task ZoekAsync()
        {
            await LoadBooksFromApiWithFiltersAsync();
        }

        private void Nieuw()
        {
            SelectedBoek = null;
            Titel = string.Empty;
            Auteur = string.Empty;
            Isbn = string.Empty;
            CategorieId = 0;
            SelectedCategory = null;
            ValidationMessage = string.Empty;
            ClearErrors();
            RaiseFieldErrorProperties();
        }

        private void BuildValidationMessage()
        {
            var props = new[] { nameof(Titel), nameof(Auteur), nameof(Isbn) };
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
            OnPropertyChanged(nameof(TitelError));
            OnPropertyChanged(nameof(AuteurError));
            OnPropertyChanged(nameof(IsbnError));
            OnPropertyChanged(nameof(HasValidationErrors));
        }

        private async Task OpslaanAsync()
        {
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
                if (_sync != null)
                {
                    if (SelectedBoek == null)
                    {
                        var nieuw = new Boek { Titel = Titel, Auteur = Auteur, Isbn = Isbn, CategorieID = CategorieId };
                        var created = await _sync.CreateBoekAsync(nieuw);
                        if (created != null) await LoadBooksAsync();
                    }
                    else
                    {
                        SelectedBoek.Titel = Titel;
                        SelectedBoek.Auteur = Auteur;
                        SelectedBoek.Isbn = Isbn;
                        SelectedBoek.CategorieID = CategorieId;
                        await _sync.UpdateBoekAsync(SelectedBoek);
                        await LoadBooksAsync();
                    }
                }
                else
                {
                    using var db = _dbFactory.CreateDbContext();
                    if (SelectedBoek == null)
                    {
                        var nieuw = new Boek { Titel = Titel, Auteur = Auteur, Isbn = Isbn, CategorieID = CategorieId };
                        db.Boeken.Add(nieuw);
                    }
                    else
                    {
                        var existing = await db.Boeken.FindAsync(SelectedBoek.Id);
                        if (existing != null)
                        {
                            existing.Titel = Titel;
                            existing.Auteur = Auteur;
                            existing.Isbn = Isbn;
                            existing.CategorieID = CategorieId;
                            db.Boeken.Update(existing);
                        }
                    }
                    await db.SaveChangesAsync();
                    await LoadBooksAsync();
                }

                SelectedBoek = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Boek opgeslagen.");
            }
            catch (Exception ex)
            {
                ValidationMessage = "Fout bij opslaan.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
            }
        }

        private async Task VerwijderAsync()
        {
            ValidationMessage = string.Empty;
            if (SelectedBoek == null) return;
            try
            {
                if (_sync != null)
                {
                    await _sync.DeleteBoekAsync(SelectedBoek.Id);
                    await LoadBooksAsync();
                }
                else
                {
                    using var db = _dbFactory.CreateDbContext();
                    var existing = await db.Boeken.FindAsync(SelectedBoek.Id);
                    if (existing != null)
                    {
                        existing.IsDeleted = true;
                        db.Boeken.Update(existing);
                        await db.SaveChangesAsync();
                    }
                    await LoadBooksAsync();
                }

                SelectedBoek = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Boek verwijderd.");
            }
            catch (Exception ex)
            {
                ValidationMessage = "Fout bij verwijderen.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
            }
        }

        private async Task NavigateToDetailsAsync(Boek? b)
        {
            if (b == null) return;
            try
            {
                await Shell.Current.GoToAsync($"{nameof(Pages.BoekDetailsPage)}?boekId={b.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", "Kan detailspagina niet openen.");
            }
        }

        private async Task DeleteItemAsync(Boek? b)
        {
            if (b == null) return;
            try
            {
                if (_sync != null)
                {
                    await _sync.DeleteBoekAsync(b.Id);
                    await LoadBooksAsync();
                    await ShowAlertAsync("Gereed", "Boek verwijderd.");
                    return;
                }

                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Boeken.FindAsync(b.Id);
                if (existing != null)
                {
                    existing.IsDeleted = true;
                    db.Boeken.Update(existing);
                    await db.SaveChangesAsync();
                }

                await LoadBooksAsync();
                await ShowAlertAsync("Gereed", "Boek verwijderd.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", "Fout bij verwijderen boek.");
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
            catch { }
        }

        // public helper to ensure categories/books are loaded when page appears
        public async Task EnsureCategoriesLoadedAsync()
        {
            try
            {
                await LoadCategoriesAsync();
                await LoadBooksAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}