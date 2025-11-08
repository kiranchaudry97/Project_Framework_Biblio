using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Page
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void OnLogin(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text?.Trim();
            var pwd = PwdBox.Visibility == Visibility.Visible ? PwdBox.Password : PwdTextBox.Text;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
            {
                MessageBox.Show("E-mail en wachtwoord zijn verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var services = Biblio_WPF.App.AppHost?.Services;
            if (services == null) return;

            var userManager = services.GetService<UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
            // use AppUser type if present
            var um = services.GetService<UserManager<Biblio_Models.Entiteiten.AppUser>>();
            var userManagerApp = um;
            if (userManagerApp == null)
            {
                MessageBox.Show("User manager niet beschikbaar.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var user = await userManagerApp.FindByEmailAsync(email);
            if (user == null)
            {
                MessageBox.Show("Onbekende gebruiker.", "Inloggen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (user.IsBlocked)
            {
                MessageBox.Show("Deze gebruiker is geblokkeerd.", "Inloggen", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pwdValid = await userManagerApp.CheckPasswordAsync(user, pwd);
            if (!pwdValid)
            {
                MessageBox.Show("Ongeldig wachtwoord.", "Inloggen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Set security context
            var security = services.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
            security?.SetUser(user.Email, await userManagerApp.IsInRoleAsync(user, "Admin"), await userManagerApp.IsInRoleAsync(user, "Medewerker"));

            MessageBox.Show("Inloggen gelukt.", "Inloggen", MessageBoxButton.OK, MessageBoxImage.Information);
            // Close the login page window if hosted in a Window
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }

        private void OnShowPasswordChecked(object sender, RoutedEventArgs e)
        {
            PwdTextBox.Text = PwdBox.Password;
            PwdTextBox.Visibility = Visibility.Visible;
            PwdBox.Visibility = Visibility.Collapsed;
        }

        private void OnShowPasswordUnchecked(object sender, RoutedEventArgs e)
        {
            PwdBox.Password = PwdTextBox.Text;
            PwdTextBox.Visibility = Visibility.Collapsed;
            PwdBox.Visibility = Visibility.Visible;
        }

        private void OnForgotPassword(object sender, RoutedEventArgs e)
        {
            var services = Biblio_WPF.App.AppHost?.Services;
            var page = services?.GetService<ResetWindow>();
            if (page != null)
            {
                var w = new System.Windows.Window { Title = "Wachtwoord vergeten", Content = page, Owner = System.Windows.Window.GetWindow(this), Width = 520, Height = 260 };
                w.ShowDialog();
            }
        }
    }
}
