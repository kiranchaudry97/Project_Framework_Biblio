using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using Biblio_App.Models;
using System.Threading.Tasks;
using System.Linq;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using Biblio_App.Services; // <-- IGegevensProvider
using System.Resources;
using System.Globalization;
using System;
using Microsoft.Maui.ApplicationModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Biblio_App.ViewModels
{
    public class CategorieenViewModel : INotifyPropertyChanged, Biblio_App.Services.ILocalizable
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<LocalDbContext> _dbFactory;
        private readonly ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;
        private bool _resourceManagerInitialized = false;

        public ObservableCollection<Categorie> Categorien { get; } = new ObservableCollection<Categorie>();

        public CategorieenViewModel(IDbContextFactory<LocalDbContext> dbFactory, IGegevensProvider? gegevensProvider = null, ILanguageService? languageService = null)
        {
            _dbFactory = dbFactory ?? throw new System.ArgumentNullException(nameof(dbFactory));
            _gegevensProvider = gegevensProvider;
            _languageService = languageService;

            _ = LoadAsync();

            try
            {
                if (_languageService != null)
                {
                    _languageService.LanguageChanged += (s, c) => Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
                }
            }
            catch { }

            // initialiseer gelokaliseerde strings
            UpdateLocalizedStrings();

            // initialiseer commando's
            NewCommand = new Command(async () => await AddCategoryAsync(), () => !string.IsNullOrWhiteSpace(NewCategoryName));
            DeleteCommand = new Command(async () => await DeleteSelectedAsync(), () => SelectedCategorie != null);
            SaveCommand = new Command(async () => await SaveSelectedAsync(), () => SelectedCategorie != null && !string.IsNullOrWhiteSpace(NewCategoryName));
        }

        // Gelokaliseerde UI eigenschappen
        private string _pageHeaderText = string.Empty;
        public string PageHeaderText { get => _pageHeaderText; set { _pageHeaderText = value; OnPropertyChanged(nameof(PageHeaderText)); } }

        private string _namePlaceholder = string.Empty;
        public string NamePlaceholder { get => _namePlaceholder; set { _namePlaceholder = value; OnPropertyChanged(nameof(NamePlaceholder)); } }

        private string _newButtonText = string.Empty;
        public string NewButtonText { get => _newButtonText; set { _newButtonText = value; OnPropertyChanged(nameof(NewButtonText)); } }

        private string _deleteButtonText = string.Empty;
        public string DeleteButtonText { get => _deleteButtonText; set { _deleteButtonText = value; OnPropertyChanged(nameof(DeleteButtonText)); } }

        private string _saveButtonText = string.Empty;
        public string SaveButtonText { get => _saveButtonText; set { _saveButtonText = value; OnPropertyChanged(nameof(SaveButtonText)); } }

        // Breadcrumb / overzicht gelokaliseerde strings
        private string _overviewText = string.Empty;
        public string OverviewText { get => _overviewText; set { _overviewText = value; OnPropertyChanged(nameof(OverviewText)); } }

        private string _detailsLabel = string.Empty;
        public string DetailsLabel { get => _detailsLabel; set { _detailsLabel = value; OnPropertyChanged(nameof(DetailsLabel)); } }

        private string _editLabel = string.Empty;
        public string EditLabel { get => _editLabel; set { _editLabel = value; OnPropertyChanged(nameof(EditLabel)); } }

        private string _deleteLabel = string.Empty;
        public string DeleteLabel { get => _deleteLabel; set { _deleteLabel = value; OnPropertyChanged(nameof(DeleteLabel)); } }

        // Nieuwe categorie naam ingevoerd in Entry
        private string _newCategoryName = string.Empty;
        public string NewCategoryName
        {
            get => _newCategoryName;
            set
            {
                _newCategoryName = value;
                OnPropertyChanged(nameof(NewCategoryName));
                if (NewCommand is Command nc) nc.ChangeCanExecute();
                if (SaveCommand is Command sc) sc.ChangeCanExecute();
            }
        }

        // Huidige geselecteerde categorie in de lijst
        private Categorie? _selectedCategorie;
        public Categorie? SelectedCategorie
        {
            get => _selectedCategorie;
            set
            {
                _selectedCategorie = value;
                OnPropertyChanged(nameof(SelectedCategorie));
                if (DeleteCommand is Command dc) dc.ChangeCanExecute();
                if (SaveCommand is Command sc) sc.ChangeCanExecute();
                if (value != null) NewCategoryName = value.Naam;
            }
        }

        // Commands
        public ICommand NewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }

        private async Task LoadAsync()
        {
            Categorien.Clear();
            Categorien.Add(new Categorie { Id = 0, Naam = "Alle" });
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var cats = await db.Categorien.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync();
                foreach (var c in cats) Categorien.Add(c);
            }
            catch { }
            OnPropertyChanged(nameof(Categorien));
        }

        private async Task AddCategoryAsync()
        {
            try
            {
                var name = (NewCategoryName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name)) return;

                using var db = _dbFactory.CreateDbContext();
                var cat = new Categorie { Naam = name };
                db.Categorien.Add(cat);
                await db.SaveChangesAsync();

                // reload or add to collection
                Categorien.Add(cat);

                // clear input
                NewCategoryName = string.Empty;
            }
            catch { }
        }

        private async Task DeleteSelectedAsync()
        {
            try
            {
                if (SelectedCategorie == null) return;

                using var db = _dbFactory.CreateDbContext();
                var entity = await db.Categorien.FindAsync(SelectedCategorie.Id);
                if (entity != null)
                {
                    // soft delete if property exists
                    try { entity.IsDeleted = true; db.Categorien.Update(entity); }
                    catch { db.Categorien.Remove(entity); }
                    await db.SaveChangesAsync();
                }

                Categorien.Remove(SelectedCategorie);
                SelectedCategorie = null;
                NewCategoryName = string.Empty;
            }
            catch { }
        }

        private async Task SaveSelectedAsync()
        {
            try
            {
                if (SelectedCategorie == null) return;
                var name = (NewCategoryName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name)) return;

                using var db = _dbFactory.CreateDbContext();
                var entity = await db.Categorien.FindAsync(SelectedCategorie.Id);
                if (entity != null)
                {
                    entity.Naam = name;
                    db.Categorien.Update(entity);
                    await db.SaveChangesAsync();
                }

                // update in-memory collection
                SelectedCategorie.Naam = name;
                OnPropertyChanged(nameof(Categorien));
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ILocalizable implementation
        public void UpdateLocalizedStrings()
        {
            try
            {
                EnsureResourceManagerInitialized();
                 // Prefer AppShell translation when available
                 string Localize(string key)
                 {
                     try
                     {
                         var shell = AppShell.Instance;
                         if (shell != null)
                         {
                             var val = shell.Translate(key);
                             if (!string.IsNullOrEmpty(val)) return val;
                         }
                     }
                     catch { }

                    var culture = _languageService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                    // try ResourceManager first
                    if (_sharedResourceManager != null)
                    {
                        try
                        {
                            var val = _sharedResourceManager.GetString(key, culture);
                            if (!string.IsNullOrEmpty(val)) return val;
                        }
                        catch { }
                    }

                    // simple fallback mapping
                    var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                    if (code == "en")
                    {
                        return key switch
                        {
                            "Categories" => "Categories",
                            "NamePlaceholder" => "Name",
                            "New" => "New",
                            "Delete" => "Delete",
                            "Save" => "Save",
                            "Overview" => "Overview",
                            "Details" => "Details",
                            "Edit" => "Edit",
                            _ => key
                        };
                    }

                    if (code == "fr")
                    {
                        return key switch
                        {
                            "Categories" => "Catégories",
                            "NamePlaceholder" => "Nom",
                            "New" => "Nouveau",
                            "Delete" => "Supprimer",
                            "Save" => "Enregistrer",
                            "Overview" => "Aperçu",
                            "Details" => "Détails",
                            "Edit" => "Modifier",
                            _ => key
                        };
                    }

                    // Dutch
                    return key switch
                    {
                        "Categories" => "Categorieën",
                        "NamePlaceholder" => "Voeg categorie toe",
                        "New" => "Nieuw",
                        "Delete" => "Verwijder",
                        "Save" => "Opslaan",
                        "Overview" => "Overzicht",
                        "Details" => "Details",
                        "Edit" => "Bewerk",
                        _ => key
                    };
                 }

                 PageHeaderText = Localize("Categories");

                var namePlaceholderValue = Localize("NamePlaceholder");
                if (string.IsNullOrEmpty(namePlaceholderValue) || namePlaceholderValue == "NamePlaceholder")
                {
                    // fallback to a sensible language-specific default if lookup failed
                    try
                    {
                        var cultureCode = (_languageService?.CurrentCulture ?? System.Globalization.CultureInfo.CurrentUICulture).TwoLetterISOLanguageName.ToLowerInvariant();
                        if (cultureCode == "nl") namePlaceholderValue = "Voeg categorie toe";
                        else if (cultureCode == "fr") namePlaceholderValue = "Ajouter une catégorie";
                        else if (cultureCode == "en") namePlaceholderValue = "Add category";
                        else namePlaceholderValue = "Add category";
                    }
                    catch { namePlaceholderValue = "Add category"; }
                }

                NamePlaceholder = namePlaceholderValue;
                NewButtonText = Localize("New");
                DeleteButtonText = Localize("Delete");
                SaveButtonText = Localize("Save");

                // breadcrumb / overview labels
                OverviewText = Localize("Overview");
                DetailsLabel = Localize("Details");
                EditLabel = Localize("Edit");
                DeleteLabel = Localize("Delete");
            }
            catch { }
        }

        private void EnsureResourceManagerInitialized()
        {
            if (_resourceManagerInitialized) return;
            _resourceManagerInitialized = true;
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (asm != null)
                {
                    var candidates = new[] {
                        "Biblio_Web.Resources.Vertalingen.SharedResource",
                        "Biblio_Web.Resources.SharedResource",
                        "Biblio_Web.SharedResource"
                    };
                    foreach (var baseName in candidates)
                    {
                        try
                        {
                            var rm = new ResourceManager(baseName, asm);
                            var test = rm.GetString("Categories", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test)) { _sharedResourceManager = rm; return; }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                _sharedResourceManager = Biblio_Models.Resources.SharedModelResource.ResourceManager;
            }
            catch { }
        }
    }
}