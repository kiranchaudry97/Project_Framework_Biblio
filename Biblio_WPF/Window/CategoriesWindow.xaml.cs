// Doel: Bootstrap van WPF-app met generieke host, DI, EF Core, Identity en seeding.
// Beschrijving: Start de Host, configureert services (DbContext, Identity, ViewModels/Windows),
// voert database seeding uit en toont het hoofdvenster. Bevat globale exception handlers.
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for CategoriesWindow.xaml
    /// </summary>
    public partial class CategoriesWindow : Page
    {
        public CategoriesWindow()
        {
            InitializeComponent();
            Loaded += CategoriesWindow_Loaded;
        }

        private async void CategoriesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCategories();
        }

        private async System.Threading.Tasks.Task LoadCategories()
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var categories = await db.Categorien.Where(c => !c.IsDeleted).OrderBy(c => c.Naam).ToListAsync();
            CategoriesGrid.ItemsSource = categories;
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }

        private async void OnAddCategory(object sender, RoutedEventArgs e)
        {
            var name = CategoryNameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Voer een categorie naam in."); return; }

            var svc = Biblio_WPF.App.AppHost?.Services;
            var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            if (await db.Categorien.AnyAsync(c => c.Naam == name && !c.IsDeleted))
            {
                MessageBox.Show("Categorie bestaat al.");
                return;
            }

            var cat = new Biblio_Models.Entiteiten.Categorie { Naam = name };
            db.Categorien.Add(cat);
            await db.SaveChangesAsync();
            CategoryNameBox.Text = string.Empty;
            await LoadCategories();
        }

        private async void OnDeleteCategory(object sender, RoutedEventArgs e)
        {
            var sel = CategoriesGrid.SelectedItem as Biblio_Models.Entiteiten.Categorie;
            if (sel == null) { MessageBox.Show("Selecteer een categorie."); return; }
            if (MessageBox.Show($"Verwijder categorie '{sel.Naam}'? (soft delete)", "Bevestigen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var svc = Biblio_WPF.App.AppHost?.Services;
                var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
                if (db == null) return;
                sel.IsDeleted = true;
                db.Categorien.Update(sel);
                await db.SaveChangesAsync();
                await LoadCategories();
            }
        }
    }
}
