using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

namespace Biblio_App.Pages
{
    public partial class LedenPagina : ContentPage
    {
        public LedenPagina(LedenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
