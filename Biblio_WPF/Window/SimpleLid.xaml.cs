// Doel: Bootstrap van WPF-app met generieke host, DI, EF Core, Identity en seeding.
// Beschrijving: Start de Host, configureert services (DbContext, Identity, ViewModels/Windows),
// voert database seeding uit en toont het hoofdvenster. Bevat globale exception handlers.
// zie commit bericht
using System.Windows;

namespace Biblio_WPF.Window
{
    public partial class SimpleLid : System.Windows.Window
    {
        public Biblio_Models.Entiteiten.Lid Member { get; private set; }

        public SimpleLid()
        {
            InitializeComponent();
            Member = new Biblio_Models.Entiteiten.Lid();
        }

        public SimpleLid(Biblio_Models.Entiteiten.Lid existing) : this()
        {
            Member = existing;
            if (VoornaamBox != null) VoornaamBox.Text = Member.Voornaam ?? string.Empty;
            if (NaamBox != null) NaamBox.Text = Member.AchterNaam ?? string.Empty;
            if (EmailBox != null) EmailBox.Text = Member.Email ?? string.Empty;
            if (TelBox != null) TelBox.Text = Member.Telefoon ?? string.Empty;
        }

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

        private void OnOk(object sender, RoutedEventArgs e)
        {
            Member.Voornaam = (VoornaamBox != null ? VoornaamBox.Text?.Trim() ?? string.Empty : Member.Voornaam);
            Member.AchterNaam = (NaamBox != null ? NaamBox.Text?.Trim() ?? string.Empty : Member.AchterNaam);
            Member.Email = (EmailBox != null ? EmailBox.Text?.Trim() ?? string.Empty : Member.Email);
            Member.Telefoon = (TelBox != null ? TelBox.Text?.Trim() ?? string.Empty : Member.Telefoon);
            DialogResult = true;
        }
    }
}