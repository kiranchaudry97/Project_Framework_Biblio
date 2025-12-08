using Microsoft.Maui.Controls;

namespace Biblio_App.Pages
{
    public partial class LidDetailsPage : ContentPage
    {
        public LidDetailsPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
