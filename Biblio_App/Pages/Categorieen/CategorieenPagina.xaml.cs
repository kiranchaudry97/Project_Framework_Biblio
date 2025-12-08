using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

namespace Biblio_App.Pages
{
    public partial class CategorieenPagina : ContentPage
    {
        public CategorieenPagina(CategorieenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
