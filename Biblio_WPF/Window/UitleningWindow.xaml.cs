using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Biblio_WPF.Window
{
    public partial class UitleningWindow : Page
    {
        public UitleningWindow()
        {
            InitializeComponent();
            Loaded += UitleningWindow_Loaded;
        }

        private async void UitleningWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFilters();
            await LoadLoans();
        }

        private async System.Threading.Tasks.Task LoadFilters()
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var leden = await db.Leden.Where(l => !l.IsDeleted).OrderBy(l => l.Voornaam).ToListAsync();
            LidFilter.ItemsSource = leden;
            LidFilter.DisplayMemberPath = "Voornaam";

            var boeken = await db.Boeken.Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();
            BoekFilter.ItemsSource = boeken;
            BoekFilter.DisplayMemberPath = "Titel";
        }

        private async System.Threading.Tasks.Task LoadLoans()
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var q = db.Leningens.Include(u => u.Boek).Include(u => u.Lid).Where(u => !u.IsDeleted).AsQueryable();

            if (LidFilter.SelectedItem is Biblio_Models.Entiteiten.Lid lid)
                q = q.Where(u => u.LidId == lid.Id);

            if (BoekFilter.SelectedItem is Biblio_Models.Entiteiten.Boek boek)
                q = q.Where(u => u.BoekId == boek.Id);

            if (OnlyOpenCheck.IsChecked == true)
                q = q.Where(u => u.ReturnedAt == null);

            // Example of LINQ query syntax (requirement): find overdue open loans
            var overdueQuery = from u in db.Leningens
                               where !u.IsDeleted && u.ReturnedAt == null && u.DueDate < DateTime.Now
                               select u;

            var overdueCount = await overdueQuery.CountAsync();

            var list = await q.OrderByDescending(u => u.StartDate).ToListAsync();
            LoansGrid.ItemsSource = list;
            CountLabel.Text = $"{list.Count} uitleningen (achterstallig: {overdueCount})";
        }

        private async void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            await LoadLoans();
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            // navigate back (if needed)
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }

        private void OnNewLoan(object sender, RoutedEventArgs e)
        {
            var dlg = new SimpleUitleningDialoog();
            if (dlg.ShowDialog() == true)
            {
                _ = CreateLoanAsync(dlg.Loan);
            }
        }

        private async System.Threading.Tasks.Task CreateLoanAsync(Biblio_Models.Entiteiten.Lenen loan)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;
            db.Leningens.Add(loan);
            await db.SaveChangesAsync();
            await LoadLoans();
        }

        private async void OnReturnLoan(object sender, RoutedEventArgs e)
        {
            var sel = LoansGrid.SelectedItem as Biblio_Models.Entiteiten.Lenen;
            if (sel == null) { MessageBox.Show("Selecteer een uitlening."); return; }
            if (MessageBox.Show($"Bevestig inleveren van '{sel.Boek?.Titel}'?", "Bevestigen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var svc = Biblio_WPF.App.AppHost?.Services;
                var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
                if (db == null) return;
                sel.ReturnedAt = DateTime.Now;
                sel.IsClosed = true;
                db.Leningens.Update(sel);
                await db.SaveChangesAsync();
                await LoadLoans();
            }
        }

        private async void OnDeleteLoan(object sender, RoutedEventArgs e)
        {
            var sel = LoansGrid.SelectedItem as Biblio_Models.Entiteiten.Lenen;
            if (sel == null) { MessageBox.Show("Selecteer een uitlening."); return; }
            if (MessageBox.Show($"Verwijder uitlening? (soft delete)", "Bevestigen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var svc = Biblio_WPF.App.AppHost?.Services;
                var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
                if (db == null) return;
                sel.IsDeleted = true;
                db.Leningens.Update(sel);
                await db.SaveChangesAsync();
                await LoadLoans();
            }
        }
    }
}
