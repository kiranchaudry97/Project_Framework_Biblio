using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using Biblio_App.Models;
using System.Threading.Tasks;
using System.Linq;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using Biblio_App.Services; // <-- IGegevensProvider

namespace Biblio_App.ViewModels
{
    public class CategorieenViewModel : INotifyPropertyChanged
    {
        private readonly IGegevensProvider? _gegevensProvider;
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;

        public ObservableCollection<Categorie> Categorien { get; } = new ObservableCollection<Categorie>();

        public CategorieenViewModel(IDbContextFactory<BiblioDbContext> dbFactory, IGegevensProvider? gegevensProvider = null)
        {
            _dbFactory = dbFactory ?? throw new System.ArgumentNullException(nameof(dbFactory));
            _gegevensProvider = gegevensProvider;

            _ = LoadAsync();
        }

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
    }
}