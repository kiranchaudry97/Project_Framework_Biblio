using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Biblio_Models.Entiteiten;
using Biblio_App.Services;
using System.ComponentModel;
using System;
using System.Linq;
using Biblio_Models.Data;
using Microsoft.Maui.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Threading;
using System.Threading;
using System.Threading;

namespace Biblio_App.ViewModels
{
    public partial class BoekenViewModel : ObservableValidator
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly BiblioDbContext _db;

        private CancellationTokenSource? _searchCts;

            public ObservableCollection<Boek> Boeken { get; } = new ObservableCollection<Boek>();

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

        public BoekenViewModel(BiblioDbContext db, IGegevensProvider? gegevensProvider = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _gegevensProvider = gegevensProvider;

            NieuwCommand = new RelayCommand(Nieuw);
            OpslaanCommand = new AsyncRelayCommand(OpslaanAsync);
            VerwijderCommand = new AsyncRelayCommand(VerwijderAsync);

            LoadBooks();
        }

        partial void OnSelectedBoekChanged(Boek? value)
        {
            if (value != null)
            {
                Titel = value.Titel;
                Auteur = value.Auteur;
                Isbn = value.Isbn;
                CategorieId = value.CategorieID;
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
                ValidationMessage = string.Empty;
                ClearErrors();
                RaiseFieldErrorProperties();
            }
        }

        private void LoadBooks()
        {
            var list = _db.Boeken.AsNoTracking().ToList();
            Boeken.Clear();
            foreach (var b in list) Boeken.Add(b);
        }

        private void Nieuw()
        {
            SelectedBoek = null;
            Titel = string.Empty;
            Auteur = string.Empty;
            Isbn = string.Empty;
            CategorieId = 0;
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

            var selectedId = SelectedBoek != null ? SelectedBoek.Id : 0;
            var exists = _db.Boeken.Any(b => !string.IsNullOrEmpty(Isbn) && b.Isbn == Isbn && b.Id != selectedId);
            if (exists)
            {
                ValidationMessage = "ISBN bestaat al in de database.";
                await ShowAlertAsync("Validatie", ValidationMessage);
                return;
            }

            try
            {
                if (SelectedBoek == null)
                {
                    var nieuw = new Boek
                    {
                        Titel = Titel,
                        Auteur = Auteur,
                        Isbn = Isbn,
                        CategorieID = CategorieId
                    };
                    _db.Boeken.Add(nieuw);
                }
                else
                {
                    var existing = await _db.Boeken.FindAsync(SelectedBoek.Id);
                    if (existing != null)
                    {
                        existing.Titel = Titel;
                        existing.Auteur = Auteur;
                        existing.Isbn = Isbn;
                        existing.CategorieID = CategorieId;
                        _db.Boeken.Update(existing);
                    }
                }

                await _db.SaveChangesAsync();
                LoadBooks();
                SelectedBoek = null;
                ValidationMessage = string.Empty;
                await ShowAlertAsync("Gereed", "Boek opgeslagen.");
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
            if (SelectedBoek == null) return;
            try
            {
                var existing = await _db.Boeken.FindAsync(SelectedBoek.Id);
                if (existing != null)
                {
                    _db.Boeken.Remove(existing);
                    await _db.SaveChangesAsync();
                }

                LoadBooks();
                SelectedBoek = null;
                await ShowAlertAsync("Gereed", "Boek verwijderd.");
            }
            catch (DbUpdateException ex)
            {
                ValidationMessage = "Fout bij verwijderen. Dit boek kan gekoppeld zijn aan uitleningen.";
                System.Diagnostics.Debug.WriteLine(ex);
                await ShowAlertAsync("Fout", ValidationMessage);
            }
            catch (Exception ex)
            {
                ValidationMessage = "Onverwachte fout bij verwijderen.";
                System.Diagnostics.Debug.WriteLine(ex);
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