// 1) //LINQ - gebruikt voor filteren, sorteren en eager-loading (Where, OrderBy, Include, ToListAsync)
//    Waar: `LoadFilters()` (categorieën ophalen), `LoadBooks()` (boekenquery met filters/zoek)
//    Doel: resultaten filteren en sorteren voor UI-lijsten en dropdowns.
// 2) //lambda expressie - predicaten/selectors in LINQ-expressies
//    Waar: predicaten zoals `c => !c.IsDeleted`, `b => b.Titel.Contains(search)`
//    Doel: compacte definitie van filtercriteria en sortering.
// 3) //CRUD - Aanmaken/Bijwerken/Verwijderen (soft delete) en `SaveChangesAsync`
//    Waar: `OnSaveBook` (Add/Update + SaveChangesAsync), `OnDeleteBook` (soft delete + Update + SaveChangesAsync)
//    Doel: beheren van boeken in de database.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for BoekWindow.xaml
    /// </summary>
    public partial class BoekWindow : Page
    {
        private Biblio_Models.Entiteiten.Boek? _selected;

        public BoekWindow()
        {
            InitializeComponent();
            Loaded += BoekWindow_Loaded;
        }

        private async void BoekWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFilters(); // (1) //LINQ wordt hier gebruikt om categorieën op te halen
            await LoadBooks();   // (1) //LINQ + (2) //lambda worden hier gebruikt voor boekenquery
        }

        private async System.Threading.Tasks.Task LoadFilters()
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var cats = await db.Categorien.Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync(); // (1) //LINQ + (2) //lambda
            CategoryFilter.ItemsSource = cats;
            CategoryFilter.DisplayMemberPath = "Naam";
            CategoryBox.ItemsSource = cats;
            CategoryBox.DisplayMemberPath = "Naam";
        }

        private async System.Threading.Tasks.Task LoadBooks()
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var q = db.Boeken.Include(b => b.categorie).Where(b => !b.IsDeleted).AsQueryable(); // (1) //LINQ + (2) //lambda + Include voor eager-loading

            var search = SearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(b => b.Titel.Contains(search) || b.Auteur.Contains(search) || b.Isbn.Contains(search)); // (2) //lambda predicate
            }

            if (CategoryFilter.SelectedItem is Biblio_Models.Entiteiten.Categorie cat)
            {
                q = q.Where(b => b.CategorieID == cat.Id); // (2) //lambda predicate gebruiken voor filter
            }

            var list = await q.OrderBy(b => b.Titel).ToListAsync(); // (1) //LINQ + (2) //lambda + (3) //CRUD read
            BooksGrid.ItemsSource = list;
            CountLabel.Text = $"{list.Count} boeken";
        }

        private async void OnSearch(object sender, RoutedEventArgs e)
        {
            await LoadBooks();
        }

        private void BooksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = BooksGrid.SelectedItem as Biblio_Models.Entiteiten.Boek;
            if (_selected == null)
            {
                TitelBox.Text = string.Empty;
                AuteurBox.Text = string.Empty;
                IsbnBox.Text = string.Empty;
                CategoryBox.SelectedItem = null;
                return;
            }

            TitelBox.Text = _selected.Titel ?? string.Empty;
            AuteurBox.Text = _selected.Auteur ?? string.Empty;
            IsbnBox.Text = _selected.Isbn ?? string.Empty;
            CategoryBox.SelectedItem = CategoryBox.Items.Cast<Biblio_Models.Entiteiten.Categorie?>().FirstOrDefault(c => c != null && c.Id == _selected.CategorieID); // (2) //lambda used in FirstOrDefault
        }

        private void OnNewBook(object sender, RoutedEventArgs e)
        {
            _selected = new Biblio_Models.Entiteiten.Boek();
            TitelBox.Text = string.Empty;
            AuteurBox.Text = string.Empty;
            IsbnBox.Text = string.Empty;
            CategoryBox.SelectedItem = null;
        }

        private async void OnSaveBook(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            // ensure we have an instance
            if (_selected == null) _selected = new Biblio_Models.Entiteiten.Boek();

            _selected.Titel = TitelBox.Text?.Trim() ?? string.Empty;
            _selected.Auteur = AuteurBox.Text?.Trim() ?? string.Empty;
            _selected.Isbn = IsbnBox.Text?.Trim() ?? string.Empty;
            if (CategoryBox.SelectedItem is Biblio_Models.Entiteiten.Categorie cat)
                _selected.CategorieID = cat.Id;

            if (_selected.Id == 0)
                db.Boeken.Add(_selected); // (3) //CRUD create
            else
                db.Boeken.Update(_selected); // (3) //CRUD update

            await db.SaveChangesAsync(); // (3) //CRUD save
            await LoadBooks();
        }

        private async void OnDeleteBook(object sender, RoutedEventArgs e)
        {
            if (_selected == null) { MessageBox.Show("Selecteer een boek."); return; }
            if (MessageBox.Show($"Verwijder '{_selected.Titel}'? (soft delete)", "Bevestigen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var svc = Biblio_WPF.App.AppHost?.Services;
                var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
                if (db == null) return;
                _selected.IsDeleted = true;
                db.Boeken.Update(_selected); // (3) //CRUD soft-delete update
                await db.SaveChangesAsync(); // (3) //CRUD save
                await LoadBooks();
            }
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }
    }
}
