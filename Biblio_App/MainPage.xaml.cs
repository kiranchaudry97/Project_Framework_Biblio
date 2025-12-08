using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

namespace Biblio_App.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}