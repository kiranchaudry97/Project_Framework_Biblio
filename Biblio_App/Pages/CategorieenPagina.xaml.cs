using Biblio_App.ViewModels;

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