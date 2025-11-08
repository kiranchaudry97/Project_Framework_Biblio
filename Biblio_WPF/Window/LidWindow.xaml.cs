// Doel: Bootstrap van WPF-app met generieke host, DI, EF Core, Identity en seeding.
// Beschrijving: Start de Host, configureert services (DbContext, Identity, ViewModels/Windows),
// voert database seeding uit en toont het hoofdvenster. Bevat globale exception handlers.

// 1) //LINQ - queries gebruikt Where/OrderBy/AnyAsync/FirstOrDefault
// 2) //lambda expression - gebruikt in predicate expressions and LINQ selectors
// 3) //CRUD - Add, Update, SaveChangesAsync
// 4) //trycatch - present around save operations

using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for LidWindow.xaml
    /// </summary>
    public partial class LidWindow : Page
    {
        private Biblio_Models.Entiteiten.Lid? _selected;

        public LidWindow()
        {
            InitializeComponent();
            Loaded += LidWindow_Loaded;
        }

        private async void LidWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMembers();
        }

        private async System.Threading.Tasks.Task LoadMembers()
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var q = db.Leden.Where(l => !l.IsDeleted).AsQueryable(); // (1) //LINQ
            var search = MemberSearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(l => l.Voornaam.Contains(search) || l.AchterNaam.Contains(search) || l.Email.Contains(search)); // (2) //lambda expression

            var list = await q.OrderBy(l => l.Voornaam).ToListAsync(); // (1) //LINQ + (2) //lambda + (3) //CRUD read
            MembersGrid.ItemsSource = list;
            MembersCountLabel.Text = $"{list.Count} leden";
        }

        private void MembersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = MembersGrid.SelectedItem as Biblio_Models.Entiteiten.Lid;
            if (_selected == null)
            {
                VoornaamBox.Text = string.Empty;
                AchternaamBox.Text = string.Empty;
                EmailBox.Text = string.Empty;
                TelefoonBox.Text = string.Empty;
                return;
            }

            VoornaamBox.Text = _selected.Voornaam ?? string.Empty;
            AchternaamBox.Text = _selected.AchterNaam ?? string.Empty;
            EmailBox.Text = _selected.Email ?? string.Empty;
            TelefoonBox.Text = _selected.Telefoon ?? string.Empty;
        }

        private async void OnSearch(object sender, RoutedEventArgs e)
        {
            await LoadMembers();
        }

        private void OnNewMember(object sender, RoutedEventArgs e)
        {
            _selected = new Biblio_Models.Entiteiten.Lid();
            VoornaamBox.Text = string.Empty;
            AchternaamBox.Text = string.Empty;
            EmailBox.Text = string.Empty;
            TelefoonBox.Text = string.Empty;

            // focus first input
            VoornaamBox.Focus();
        }

        private async void OnSaveMember(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            if (_selected == null) _selected = new Biblio_Models.Entiteiten.Lid();

            // Read values from UI
            var voornaam = VoornaamBox.Text?.Trim() ?? string.Empty;
            var achternaam = AchternaamBox.Text?.Trim() ?? string.Empty;
            var email = EmailBox.Text?.Trim() ?? string.Empty;
            var telefoon = TelefoonBox.Text?.Trim() ?? string.Empty;

            // Validation: required names
            if (string.IsNullOrWhiteSpace(voornaam) || string.IsNullOrWhiteSpace(achternaam))
            {
                MessageBox.Show("Voornaam en achternaam zijn verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validation: email required and format
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("E-mail is verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emailAttr = new EmailAddressAttribute();
            if (!emailAttr.IsValid(email))
            {
                MessageBox.Show("Ongeldig e-mailadres.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Duplicate email check (exclude current record when editing)
            var exists = await db.Leden.AnyAsync(l => !l.IsDeleted && l.Email == email && ( _selected.Id == 0 || l.Id != _selected.Id )); // (1) //LINQ + (2) //lambda
            if (exists)
            {
                MessageBox.Show("E-mail is al in gebruik door een ander lid.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Assign validated values
            _selected.Voornaam = voornaam;
            _selected.AchterNaam = achternaam;
            _selected.Email = email;
            _selected.Telefoon = telefoon;

            try // (4) //trycatch start
            {
                if (_selected.Id == 0)
                    db.Leden.Add(_selected); // (3) //CRUD add
                else
                    db.Leden.Update(_selected); // (3) //CRUD update

                await db.SaveChangesAsync(); // (3) //CRUD save

                // reload list and select the saved member
                await LoadMembers();

                // select saved member in grid
                var saved = MembersGrid.Items.Cast<Biblio_Models.Entiteiten.Lid?>().FirstOrDefault(x => x != null && x.Id == _selected.Id); // (2) //lambda + (1) //LINQ
                if (saved != null)
                {
                    MembersGrid.SelectedItem = saved;
                    MembersGrid.ScrollIntoView(saved);
                }

                MessageBox.Show("Lid opgeslagen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex) // (4) //trycatch handling
            {
                MessageBox.Show($"Fout bij opslaan: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnDeleteMember(object sender, RoutedEventArgs e)
        {
            if (_selected == null) { MessageBox.Show("Selecteer een lid."); return; }
            if (MessageBox.Show($"Verwijder '{_selected.Voornaam} {_selected.AchterNaam}'? (soft delete)", "Bevestigen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var svc = Biblio_WPF.App.AppHost?.Services;
                var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
                if (db == null) return;
                _selected.IsDeleted = true;
                db.Leden.Update(_selected); // (3) //CRUD update soft-delete
                await db.SaveChangesAsync(); // (3) //CRUD save
                _selected = null;

                // clear form after delete
                VoornaamBox.Text = string.Empty;
                AchternaamBox.Text = string.Empty;
                EmailBox.Text = string.Empty;
                TelefoonBox.Text = string.Empty;

                await LoadMembers();
            }
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }
    }
}
