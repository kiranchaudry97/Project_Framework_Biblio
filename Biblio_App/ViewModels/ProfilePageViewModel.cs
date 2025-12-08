using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;

namespace Biblio_App.ViewModels
{
    public class ProfilePageViewModel : ObservableObject
    {
        public ProfilePageViewModel(SecurityViewModel security)
        {
            Email = security.CurrentEmail ?? string.Empty;
            FullName = security.FullName ?? string.Empty;
            Roles = (security.IsAdmin ? "Admin" : (security.IsStaff ? "Medewerker" : "Lid"));
        }

        public string Email { get; }
        public string FullName { get; }
        public string Roles { get; }
    }
}
