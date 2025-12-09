using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;

namespace Biblio_App.Pages.Account
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, System.EventArgs e)
        {
            // Navigeer naar de web-login of toon een korte melding aan de gebruiker.
            await DisplayAlert("Login", "Gebruik de web-app voor inloggen.", "OK");
        }
    }
}
