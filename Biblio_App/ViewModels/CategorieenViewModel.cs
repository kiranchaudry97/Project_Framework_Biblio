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

namespace Biblio_App.ViewModels
{
    public class CategorieenViewModel : INotifyPropertyChanged, Biblio_App.Services.ILocalizable
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;
        private readonly ILanguageService? _languageService;
        private ResourceManager? _sharedResourceManager;

        public ObservableCollection<Categorie> Categorien { get; } = new ObservableCollection<Categorie>();

        public CategorieenViewModel(IDbContextFactory<BiblioDbContext> dbFactory, IGegevensProvider? gegevensProvider = null, ILanguageService? languageService = null)
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

            // initialize localized strings
            UpdateLocalizedStrings();
        }

        // Localized UI properties
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ILocalizable implementation
        public void UpdateLocalizedStrings()
        {
            try
            {
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
                            _ => key
                        };
                    }

                    // Dutch
                    return key switch
                    {
                        "Categories" => "Categorieën",
                        "NamePlaceholder" => "Naam",
                        "New" => "Nieuw",
                        "Delete" => "Verwijder",
                        "Save" => "Opslaan",
                        _ => key
                    };
                }

                PageHeaderText = Localize("Categories");
                NamePlaceholder = Localize("NamePlaceholder");
                NewButtonText = Localize("New");
                DeleteButtonText = Localize("Delete");
                SaveButtonText = Localize("Save");
            }
            catch { }
        }
    }
}