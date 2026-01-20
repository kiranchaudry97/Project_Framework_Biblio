using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

namespace Biblio_App.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel viewModel)
        {
            // UI initialiseren en ViewModel binden (MVVM)
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Bij het tonen van de pagina verversen we de tellers/status.
            // try/catch zodat de app niet crasht als er bv. geen internet is
            // of als de lokale database tijdelijk niet beschikbaar is.
            try
            {
                if (BindingContext is MainViewModel vm)
                {
                    await vm.VernieuwenAsync();
                }
            }
            catch
            {
                // Best-effort: fouten negeren zodat de pagina toch blijft werken.
            }
        }
    }
}