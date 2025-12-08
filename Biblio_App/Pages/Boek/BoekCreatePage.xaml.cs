using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

namespace Biblio_App.Pages.Boek
{
    public partial class BoekCreatePage : ContentPage
    {
        public BoekCreatePage(BoekenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private async void OnCancelClicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
