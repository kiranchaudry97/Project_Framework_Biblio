using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Biblio_WPF.ViewModels;
using Biblio_WPF.Window;

namespace Biblio_WPF
{
    public partial class MainWindow : System.Windows.Window
    {
        private bool _loginShown = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);

            // sync theme toggle state and icon
            try
            {
                var toggle = this.FindName("ThemeToggleButton") as System.Windows.Controls.Primitives.ToggleButton;
                var icon = this.FindName("ThemeIcon") as TextBlock;
                var darkActive = Application.Current.Resources.MergedDictionaries.Any(d => d.Source != null && d.Source.OriginalString.Contains("Theme.Dark"));
                if (toggle != null) toggle.IsChecked = darkActive;
                if (icon != null) icon.Text = darkActive ? "\uE3F5" : "\uE706"; // moon vs view icon
            }
            catch { }

            if (_loginShown) return;
            _loginShown = true;

            var services = App.AppHost?.Services;
            var security = services?.GetService<ViewModels.SecurityViewModel>();

            if (security == null || string.IsNullOrWhiteSpace(security.CurrentEmail))
            {
                var loginPage = services?.GetService<LoginWindow>();
                if (loginPage != null)
                {
                    var w = new System.Windows.Window { Title = "Inloggen", Content = loginPage, Owner = this, Width = 520, Height = 320, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                    w.ShowDialog();
                }

                // after dialog: if still not logged in, close app
                if (security == null || string.IsNullOrWhiteSpace(security.CurrentEmail))
                {
                    this.Close();
                }
            }
        }

        private void OnThemeChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                App.UseDarkTheme();
                var icon = this.FindName("ThemeIcon") as TextBlock;
                if (icon != null) icon.Text = "\uE3F5"; // moon
            }
            catch { }
        }
        private void OnThemeUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                App.UseLightTheme();
                var icon = this.FindName("ThemeIcon") as TextBlock;
                if (icon != null) icon.Text = "\uE706"; // view icon
            }
            catch { }
        }

        private void OnOpenLogin(object sender, RoutedEventArgs e)
        {
            var svc = App.AppHost?.Services;
            if (svc == null) return;
            var loginPage = svc.GetService<LoginWindow>();
            if (loginPage == null) return;

            var w = new System.Windows.Window { Title = "Inloggen", Content = loginPage, Owner = this, Width = 520, Height = 320, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            w.ShowDialog();
        }

        private void OnOpenProfile(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var page = svc.GetService<ProfileWindow>();
            if (page != null)
            {
                var w = new System.Windows.Window { Title = "Profiel", Content = page, Owner = this, Width = 600, Height = 400 };
                w.ShowDialog();
            }
        }

        private void OnChangePassword(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var page = svc.GetService<WachtwoordVeranderenWindw>();
            if (page != null)
            {
                var w = new System.Windows.Window { Title = "Wachtwoord wijzigen", Content = page, Owner = this, Width = 600, Height = 400 };
                w.ShowDialog();
            }
        }

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            var security = svc?.GetService<SecurityViewModel>();
            security?.Reset();
            MessageBox.Show("Afgemeld.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExit(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void OnManageUsers(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var win = svc.GetService<AdminUsersWindow>();
            if (win != null)
            {
                win.Owner = this;
                win.ShowDialog();
            }
        }

        private void OnOpenBooks(object sender, RoutedEventArgs e)
        {
            OpenPageWindow<BoekWindow>("Boeken");
        }

        private void OnOpenMembers(object sender, RoutedEventArgs e)
        {
            OpenPageWindow<LidWindow>("Leden");
        }

        private void OnOpenLoans(object sender, RoutedEventArgs e)
        {
            OpenPageWindow<UitleningWindow>("Uitleningen");
        }

        private void OnOpenCategories(object sender, RoutedEventArgs e)
        {
            OpenPageWindow<CategoriesWindow>("Categorieën");
        }

        private void OpenPageWindow<TPage>(string title) where TPage : Page
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var page = svc.GetService<TPage>();
            if (page != null)
            {
                var w = new System.Windows.Window { Title = title, Content = page, Owner = this, Width = 900, Height = 600 };
                w.Show();
            }
        }
    }
}
