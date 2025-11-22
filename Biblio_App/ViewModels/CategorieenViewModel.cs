using System.Collections.ObjectModel;
using System.ComponentModel;
using Biblio_Models.Entiteiten;
using Biblio_App.Services;

namespace Biblio_App.ViewModels
{
    public class CategorieenViewModel : INotifyPropertyChanged
    {
        private readonly IGegevensProvider? _gegevensProvider;
        public ObservableCollection<Categorie> Categorien { get; } = new ObservableCollection<Categorie>();

        public CategorieenViewModel(IGegevensProvider? gegevensProvider = null)
        {
            _gegevensProvider = gegevensProvider;
            Categorien.Add(new Categorie { Naam = "Roman" });
            Categorien.Add(new Categorie { Naam = "Jeugd" });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}