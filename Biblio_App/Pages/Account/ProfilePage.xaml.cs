using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;
using Microsoft.Maui.Storage;

namespace Biblio_App.Pages.Account
{
    public partial class ProfilePage : ContentPage
    {
        private readonly SecurityViewModel _security;
        public ProfilePage(SecurityViewModel security)
        {
            InitializeComponent();
            _security = security;
            BindingContext = new ProfilePageViewModel(security);
        }

        private async void OnLogoutClicked(object sender, System.EventArgs e)
        {
            _security.Reset();
            Preferences.Default.Remove("CurrentEmail");
            Preferences.Default.Remove("IsAdmin");
            Preferences.Default.Remove("IsStaff");
            await Shell.Current.DisplayAlert("Logout", "Je bent afgemeld.", "OK");
            await Shell.Current.GoToAsync("//Home");
        }
    }
}
