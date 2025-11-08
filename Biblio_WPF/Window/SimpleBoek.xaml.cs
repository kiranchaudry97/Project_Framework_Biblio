using System.Windows;

namespace Biblio_WPF.Window
{
    public partial class SimpleBoek : System.Windows.Window
    {
        public Biblio_Models.Entiteiten.Boek Book { get; private set; }

        public SimpleBoek()
        {
            InitializeComponent();
            Book = new Biblio_Models.Entiteiten.Boek();
        }

        public SimpleBoek(Biblio_Models.Entiteiten.Boek existing) : this()
        {
            Book = existing;
            if (TitelBox != null) TitelBox.Text = Book.Titel ?? string.Empty;
            if (AuteurBox != null) AuteurBox.Text = Book.Auteur ?? string.Empty;
            if (IsbnBox != null) IsbnBox.Text = Book.Isbn ?? string.Empty;
        }

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

        private void OnOk(object sender, RoutedEventArgs e)
        {
            Book.Titel = (TitelBox != null ? TitelBox.Text?.Trim() ?? string.Empty : Book.Titel);
            Book.Auteur = (AuteurBox != null ? AuteurBox.Text?.Trim() ?? string.Empty : Book.Auteur);
            Book.Isbn = (IsbnBox != null ? IsbnBox.Text?.Trim() ?? string.Empty : Book.Isbn);
            DialogResult = true;
        }
    }
}