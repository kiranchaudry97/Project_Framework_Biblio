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
            // navigate to web login or show a message
            await DisplayAlert("Login", "Gebruik de web-app voor inloggen.", "OK");
        }
    }
}
