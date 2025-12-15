using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.ComponentModel;

namespace Biblio_App.ViewModels
{
    public class ProfilePageViewModel : ObservableObject
    {
        public ProfilePageViewModel(SecurityViewModel security)
        {
            _email = security.CurrentEmail ?? string.Empty;
            _fullName = security.FullName ?? string.Empty;
            _roles = (security.IsAdmin ? "Admin" : (security.IsStaff ? "Medewerker" : "Lid"));

            // keep viewmodel in sync when security changes
            if (security is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SecurityViewModel.CurrentEmail)) Email = security.CurrentEmail ?? string.Empty;
                    if (e.PropertyName == nameof(SecurityViewModel.FullName)) FullName = security.FullName ?? string.Empty;
                    if (e.PropertyName == nameof(SecurityViewModel.IsAdmin) || e.PropertyName == nameof(SecurityViewModel.IsStaff))
                        Roles = (security.IsAdmin ? "Admin" : (security.IsStaff ? "Medewerker" : "Lid"));
                };
            }
        }

        private string _email = string.Empty;
        private string _fullName = string.Empty;
        private string _roles = string.Empty;

        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        public string Roles { get => _roles; set => SetProperty(ref _roles, value); }
    }
}
