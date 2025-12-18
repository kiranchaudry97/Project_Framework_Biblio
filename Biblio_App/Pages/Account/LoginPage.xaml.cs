using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;

namespace Biblio_App.Pages.Account
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

#if DEBUG
            DevPanel.IsVisible = true;
#else
            DevPanel.IsVisible = false;
#endif
        }

        private async void OnLoginClicked(object sender, System.EventArgs e)
        {
            // Navigeer naar de web-login of toon een korte melding aan de gebruiker.
            await DisplayAlert("Login", "Gebruik de web-app voor inloggen.", "OK");
        }

        private void OnAutofillAdminClicked(object sender, System.EventArgs e)
        {
            EmailEntry.Text = "admin@biblio.local";
            PasswordEntry.Text = "Admin1234?";
        }

        private void OnAutofillMedewerkerClicked(object sender, System.EventArgs e)
        {
            EmailEntry.Text = "medewerk@biblio.local";
            PasswordEntry.Text = "test1234?";
        }
    }
}
