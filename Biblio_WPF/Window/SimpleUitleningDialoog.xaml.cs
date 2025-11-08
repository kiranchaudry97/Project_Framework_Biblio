// Doel: Bootstrap van WPF-app met generieke host, DI, EF Core, Identity en seeding.
// Beschrijving: Start de Host, configureert services (DbContext, Identity, ViewModels/Windows),
// voert database seeding uit en toont het hoofdvenster. Bevat globale exception handlers.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;

namespace Biblio_WPF.Window
{
    public partial class SimpleUitleningDialoog : System.Windows.Window
    {
        public Biblio_Models.Entiteiten.Lenen Loan { get; private set; }

        public SimpleUitleningDialoog()
        {
            InitializeComponent();
            Loan = new Biblio_Models.Entiteiten.Lenen();

            if (StartBox != null) StartBox.Text = DateTime.Now.ToString("yyyy-MM-dd");
            if (EndBox != null) EndBox.Text = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd");

            Loaded += SimpleUitleningDialoog_Loaded;
        }

        private async void SimpleUitleningDialoog_Loaded(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var db = svc.GetService<Biblio_Models.Data.BiblioDbContext>();
            if (db == null) return;

            var leden = await db.Leden.Where(l => !l.IsDeleted).OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
            LidCombo.ItemsSource = leden;
            LidCombo.DisplayMemberPath = "Voornaam";

            var boeken = await db.Boeken.Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();
            BoekCombo.ItemsSource = boeken;
            BoekCombo.DisplayMemberPath = "Titel";

            // if editing existing, preselect items
            if (Loan != null)
            {
                if (Loan.BoekId != default && BoekCombo.ItemsSource is System.Collections.IEnumerable bs)
                {
                    BoekCombo.SelectedItem = bs.Cast<Biblio_Models.Entiteiten.Boek>().FirstOrDefault(b => b.Id == Loan.BoekId);
                }
                if (Loan.LidId != default && LidCombo.ItemsSource is System.Collections.IEnumerable ls)
                {
                    LidCombo.SelectedItem = ls.Cast<Biblio_Models.Entiteiten.Lid>().FirstOrDefault(l => l.Id == Loan.LidId);
                }
                if (StartBox != null && Loan.StartDate != default) StartBox.Text = Loan.StartDate.ToString("yyyy-MM-dd");
                if (EndBox != null && Loan.DueDate != default) EndBox.Text = Loan.DueDate.ToString("yyyy-MM-dd");
            }
        }

        public SimpleUitleningDialoog(Biblio_Models.Entiteiten.Lenen existing) : this()
        {
            Loan = existing ?? new Biblio_Models.Entiteiten.Lenen();
        }

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

        private void OnOk(object sender, RoutedEventArgs e)
        {
            var selectedBoek = BoekCombo.SelectedItem as Biblio_Models.Entiteiten.Boek;
            var selectedLid = LidCombo.SelectedItem as Biblio_Models.Entiteiten.Lid;
            if (selectedBoek == null || selectedLid == null)
            {
                MessageBox.Show("Selecteer een lid en een boek.");
                return;
            }

            Loan.BoekId = selectedBoek.Id;
            Loan.LidId = selectedLid.Id;

            if (!DateTime.TryParse(StartBox?.Text, out var sd) || !DateTime.TryParse(EndBox?.Text, out var ed))
            {
                MessageBox.Show("Datums ongeldig.");
                return;
            }
            Loan.StartDate = sd;
            Loan.DueDate = ed;
            Loan.IsClosed = false;
            DialogResult = true;
        }
    }
}