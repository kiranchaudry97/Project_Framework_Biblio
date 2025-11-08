using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for ResetWindow.xaml
    /// </summary>
    public partial class ResetWindow : Page
    {
        public ResetWindow()
        {
            InitializeComponent();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }

        private async void OnSendReset(object sender, RoutedEventArgs e)
        {
            var email = ResetEmailBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("E-mail is verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var services = Biblio_WPF.App.AppHost?.Services;
            if (services == null) return;

            var userManager = services.GetService<UserManager<Biblio_Models.Entiteiten.AppUser>>();
            if (userManager == null)
            {
                MessageBox.Show("User manager niet beschikbaar.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ResultText.Text = "Als het e-mailadres bekend is, ontvangt u een resetlink.";
                return;
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            // In real app you send token by email. For dev, show token and a one-click reset option (not secure)
            ResultText.Text = $"Reset token (dev): {token}";
        }
    }
}
