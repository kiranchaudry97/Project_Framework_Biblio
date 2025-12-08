using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;

namespace Biblio_App.Pages.Account
{
    public partial class UsersPage : ContentPage
    {
        public UsersPage(UsersViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
