using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interactielogica voor ResetWindow.xaml
    /// Dit venster genereert een reset-token (ontwikkeling
    /// Let op: de één‑klik reset is alleen voor development/testing en niet veilig voor productie.
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
            var resetEmailBox = this.FindName("ResetEmailBox") as TextBox;
            var resultText = this.FindName("ResultText") as TextBlock;
            var tokenBox = this.FindName("TokenBox") as TextBox;
            var resetActionPanel = this.FindName("ResetActionPanel") as StackPanel;
            var resetStatusText = this.FindName("ResetStatusText") as TextBlock;

            var email = resetEmailBox?.Text?.Trim();
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
                // Toon generieke boodschap zodat we geen informatie lekken over bestaande e-mails
                if (resultText != null) resultText.Text = "Als het e-mailadres bekend is, ontvangt u een resetlink.";
                return;
            }

            // Genereer een reset-token via Identity
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            // In een echte applicatie stuur je dit token per e-mail. Voor development tonen we het token en bieden we een één‑klik hersteloptie.
            if (resultText != null) resultText.Text = $"Reset token (dev): {token}";

            // Toon het resetactie-paneel zodat ontwikkelaar direct kan testen
            if (tokenBox != null) tokenBox.Text = token;
            if (resetActionPanel != null) resetActionPanel.Visibility = Visibility.Visible;
            if (resetStatusText != null) resetStatusText.Text = string.Empty;
        }

        private async void OnApplyReset(object sender, RoutedEventArgs e)
        {
            var resetEmailBox = this.FindName("ResetEmailBox") as TextBox;
            var tokenBox = this.FindName("TokenBox") as TextBox;
            var newPwdBox = this.FindName("NewPwdBox") as PasswordBox;
            var resetStatusText = this.FindName("ResetStatusText") as TextBlock;

            var token = tokenBox?.Text;
            var newPwd = newPwdBox?.Password;
            if (resetStatusText != null) resetStatusText.Foreground = System.Windows.Media.Brushes.Gray;

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPwd))
            {
                if (resetStatusText != null) { resetStatusText.Text = "Token en nieuw wachtwoord zijn verplicht."; resetStatusText.Foreground = System.Windows.Media.Brushes.Red; }
                return;
            }

            var services = Biblio_WPF.App.AppHost?.Services;
            if (services == null) return;

            var userManager = services.GetService<UserManager<Biblio_Models.Entiteiten.AppUser>>();
            if (userManager == null)
            {
                if (resetStatusText != null) { resetStatusText.Text = "User manager niet beschikbaar."; resetStatusText.Foreground = System.Windows.Media.Brushes.Red; }
                return;
            }

            // Zoek gebruiker op basis van het opgegeven e-mailadres in het formulier
            var email = resetEmailBox?.Text?.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                if (resetStatusText != null) { resetStatusText.Text = "Gebruiker niet gevonden."; resetStatusText.Foreground = System.Windows.Media.Brushes.Red; }
                return;
            }

            try
            {
                // Probeer wachtwoord te resetten met het token
                var res = await userManager.ResetPasswordAsync(user, token!, newPwd!);
                if (res.Succeeded)
                {
                    if (resetStatusText != null) { resetStatusText.Text = "Wachtwoord succesvol gereset."; resetStatusText.Foreground = System.Windows.Media.Brushes.Green; }
                    // Kort wachten zodat gebruiker de melding ziet en daarna venster sluiten
                    await System.Threading.Tasks.Task.Delay(700);
                    var wnd = System.Windows.Window.GetWindow(this);
                    wnd?.Close();
                }
                else
                {
                    if (resetStatusText != null) { resetStatusText.Text = string.Join(';', res.Errors.Select(err => err.Description)); resetStatusText.Foreground = System.Windows.Media.Brushes.Red; }
                }
            }
            catch (System.Exception ex)
            {
                // Toon onverwachte fouten
                if (resetStatusText != null) { resetStatusText.Text = ex.Message; resetStatusText.Foreground = System.Windows.Media.Brushes.Red; }
            }
        }
    }
}
