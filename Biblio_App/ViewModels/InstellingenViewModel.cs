using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Biblio_App.Services;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Biblio_Models.Data;
using System.Data.Common;
using Microsoft.Maui.Storage;
using System.IO;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Controls;

namespace Biblio_App.ViewModels
{
    public partial class InstellingenViewModel : ObservableObject
    {
        private readonly IDataSyncService? _sync;
        private readonly IDbContextFactory<LocalDbContext>? _dbFactory;
        private readonly ILanguageService? _languageService;

        // Include French (FR) so user can switch to fr as well
        public ObservableCollection<string> Languages { get; } = new ObservableCollection<string> { "NL", "EN", "FR" };

        [ObservableProperty]
        private string selectedLanguage = "NL";

        [ObservableProperty]
        private bool autoSync = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private string databaseInfo = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public ICommand SyncNowCommand { get; }
        public ICommand CheckApiCommand { get; }

        public InstellingenViewModel(IDataSyncService? sync = null, IDbContextFactory<LocalDbContext>? dbFactory = null, ILanguageService? languageService = null)
        {
            _sync = sync;
            _dbFactory = dbFactory;
            _languageService = languageService;

            SyncNowCommand = new AsyncRelayCommand(ExecuteSyncNowAsync);
            CheckApiCommand = new AsyncRelayCommand(ExecuteCheckApiAsync);

            // Initialize SelectedLanguage from language service if available
            try
            {
                if (_languageService?.CurrentCulture != null)
                {
                    SelectedLanguage = _languageService.CurrentCulture.TwoLetterISOLanguageName.ToUpperInvariant();
                }
                else
                {
                    // fallback to preferences
                    var code = Preferences.Default.ContainsKey("biblio-culture") ? Preferences.Default.Get("biblio-culture", "nl") : "nl";
                    SelectedLanguage = code.ToUpperInvariant();
                }

                if (_languageService != null)
                {
                    _languageService.LanguageChanged += LanguageChangedHandler;
                }
            }
            catch { }

        }

        // Keep the VM in sync when language service fires
        private void LanguageChangedHandler(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                var code = culture.TwoLetterISOLanguageName.ToUpperInvariant();
                if (SelectedLanguage != code)
                {
                    SelectedLanguage = code;
                }
            }
            catch { }
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var code = value.Trim().ToLowerInvariant();
                if (_languageService != null)
                {
                    _languageService.SetLanguage(code);
                }
                else
                {
                    try { Preferences.Default.Set("biblio-culture", code); } catch { }
                    var culture = new System.Globalization.CultureInfo(code);
                    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

                    // Refresh UI by recreating main page (best-effort)
                    try { Application.Current.MainPage = new AppShell(); } catch { }
                }
            }
            catch { }
        }

        private async Task ExecuteSyncNowAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (_sync == null)
                {
                    StatusMessage = "Geen sync-service beschikbaar.";
                    return;
                }

                StatusMessage = "Synchroniseren...";
                await _sync.SyncAllAsync();
                StatusMessage = "Synchronisatie voltooid.";

                // show a short alert (acts like a toast)
                try
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Synchronisatie", "Synchronisatie voltooid.", "OK");
                        }
                    });
                }
                catch { }

                // refresh DB info after sync
                await LoadDatabaseInfoAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Fout bij synchronisatie: " + ex.Message;
                try
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Synchronisatie", "Fout bij synchronisatie: " + ex.Message, "OK");
                        }
                    });
                }
                catch { }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteCheckApiAsync()
        {
            StatusMessage = "Controleren API... (disabled)";
            await Task.Delay(200);
            StatusMessage = "Controle niet beschikbaar in deze build.";
        }

        [RelayCommand]
        public async Task LoadDatabaseInfoAsync()
        {
            try
            {
                if (_dbFactory == null)
                {
                    DatabaseInfo = "Geen DbContextFactory geregistreerd.";
                    return;
                }

                using var db = _dbFactory.CreateDbContext();
                var provider = db.Database.ProviderName ?? "(onbekend provider)";
                string conn = "(geen verbindingstring beschikbaar)";
                try
                {
                    var c = db.Database.GetDbConnection();
                    if (c != null)
                    {
                        // ensure closed state for safety
                        if (c.State != System.Data.ConnectionState.Closed) c.Close();
                        conn = c.ConnectionString ?? conn;
                    }
                }
                catch { /* ignore retrieving connection string errors */ }

                DatabaseInfo = $"Provider: {provider}\nConnection: {conn}";
            }
            catch (Exception ex)
            {
                DatabaseInfo = "Fout bij lezen DB-config: " + ex.Message;
            }
        }

        public async Task<bool> ResetAndSeedLocalDatabaseAsync()
        {
            try
            {
                // If using SQLite local file, delete it
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "biblio.db");
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                // Create a context to force migrations and seed
                if (_dbFactory != null)
                {
                    using var db = _dbFactory.CreateDbContext();
                    await db.Database.MigrateAsync();

                    // minimal seed similar to MauiProgram.InitializeDatabaseAsync
                    if (!await db.Categorien.AnyAsync())
                    {
                        db.Categorien.AddRange(
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Roman" },
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Jeugd" },
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Thriller" },
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Wetenschap" }
                        );
                        await db.SaveChangesAsync();
                    }

                    if (!await db.Boeken.AnyAsync())
                    {
                        var roman = await db.Categorien.FirstAsync(c => c.Naam == "Roman");
                        var jeugd = await db.Categorien.FirstAsync(c => c.Naam == "Jeugd");

                        db.Boeken.AddRange(
                            new Biblio_Models.Entiteiten.Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id }
                        );
                        await db.SaveChangesAsync();
                    }

                    if (!await db.Leden.AnyAsync())
                    {
                        db.Leden.AddRange(
                            new Biblio_Models.Entiteiten.Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com" },
                            new Biblio_Models.Entiteiten.Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com" }
                        );
                        await db.SaveChangesAsync();
                    }

                    return true;
                }

                return false;
            }
            catch {
                return false;
            }
        }
    }
}
