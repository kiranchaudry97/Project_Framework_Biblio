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

            var q = db.Leden.Where(l => !l.IsDeleted).AsQueryable();
            var search = MemberSearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(l => l.Voornaam.Contains(search) || l.AchterNaam.Contains(search) || l.Email.Contains(search));

            var list = await q.OrderBy(l => l.Voornaam).ToListAsync();
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
        }

        private async void OnSaveMember(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            var db = svc?.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            if (_selected == null) _selected = new Biblio_Models.Entiteiten.Lid();

            _selected.Voornaam = VoornaamBox.Text?.Trim() ?? string.Empty;
            _selected.AchterNaam = AchternaamBox.Text?.Trim() ?? string.Empty;
            _selected.Email = EmailBox.Text?.Trim() ?? string.Empty;
            _selected.Telefoon = TelefoonBox.Text?.Trim() ?? string.Empty;

            if (_selected.Id == 0)
                db.Leden.Add(_selected);
            else
                db.Leden.Update(_selected);

            await db.SaveChangesAsync();
            await LoadMembers();
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
                db.Leden.Update(_selected);
                await db.SaveChangesAsync();
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
