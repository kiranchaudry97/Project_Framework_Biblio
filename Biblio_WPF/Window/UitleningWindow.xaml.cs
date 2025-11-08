// PATTERNS: // LINQ, // lambda, // CRUD
// UitleningWindow: uses LINQ (method- and query-syntax), lambda expressions and performs CRUD on loans (Add/Update/SaveChangesAsync).

using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

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

            // date range filtering (Van / Tot)
            bool dateValid = true;
            string filterInfo = string.Empty;

            if (FromDatePicker?.SelectedDate is DateTime fromDate)
            {
                var from = fromDate.Date;
                q = q.Where(u => u.StartDate >= from);
                filterInfo = $"Vanaf {from:d}";
            }

            if (ToDatePicker?.SelectedDate is DateTime toDate)
            {
                // include the whole day for the 'to' date
                var to = toDate.Date.AddDays(1).AddTicks(-1);
                q = q.Where(u => u.StartDate <= to);
                filterInfo = string.IsNullOrEmpty(filterInfo) ? $"Tot {toDate:d}" : filterInfo + $" - tot {toDate:d}";
            }

            if (FromDatePicker?.SelectedDate is DateTime f && ToDatePicker?.SelectedDate is DateTime t && f > t)
            {
                dateValid = false;
            }

            // Example of LINQ query syntax (requirement): find overdue open loans
            var overdueQuery = from u in db.Leningens
                               where !u.IsDeleted && u.ReturnedAt == null && u.DueDate < DateTime.Now
                               select u;

            var overdueCount = await overdueQuery.CountAsync();

            var list = await q.OrderByDescending(u => u.StartDate).ToListAsync();
            LoansGrid.ItemsSource = list;
            CountLabel.Text = $"{list.Count} uitleningen (achterstallig: {overdueCount})";

            if (!dateValid)
            {
                FilterInfoLabel.Text = "Ongeldige datumbereik";
                FilterInfoLabel.Foreground = System.Windows.Media.Brushes.DarkRed;
            }
            else
            {
                FilterInfoLabel.Text = filterInfo;
                FilterInfoLabel.Foreground = System.Windows.Media.Brushes.Gray;
            }
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
            // Remove modal dialog; create loan from selected Lid and Boek with default dates
            var selectedLid = LidFilter.SelectedItem as Biblio_Models.Entiteiten.Lid;
            var selectedBoek = BoekFilter.SelectedItem as Biblio_Models.Entiteiten.Boek;

            if (selectedLid == null || selectedBoek == null)
            {
                MessageBox.Show("Selecteer een lid en een boek voordat u uitleent.", "Informatie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var start = DateTime.Now.Date;
            var due = start.AddDays(14);

            if (MessageBox.Show($"Bevestig uitlenen van '{selectedBoek.Titel}' aan {selectedLid.Voornaam} {selectedLid.AchterNaam} tot {due:d}?", "Bevestigen", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var loan = new Biblio_Models.Entiteiten.Lenen
                {
                    LidId = selectedLid.Id,
                    BoekId = selectedBoek.Id,
                    StartDate = start,
                    DueDate = due,
                    IsClosed = false
                };

                _ = CreateLoanAsync(loan);
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
