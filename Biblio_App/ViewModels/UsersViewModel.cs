using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Biblio_App.ViewModels
{
    public class UsersViewModel : ObservableObject
    {
        public ObservableCollection<UserItem> Users { get; } = new ObservableCollection<UserItem>();

        private UserItem? _selectedUser;
        public UserItem? SelectedUser
        {
            get => _selectedUser; set => SetProperty(ref _selectedUser, value);
        }

        public IAsyncRelayCommand SaveRolesCommand { get; }
        public IRelayCommand NewCommand { get; }

        public UsersViewModel()
        {
            // sample
            Users.Add(new UserItem { Email = "admin@biblio.local", FullName = "Beheerder", IsAdmin = true, IsStaff = true });
            Users.Add(new UserItem { Email = "user@example.com", FullName = "Gebruiker", IsAdmin = false, IsStaff = false });

            SaveRolesCommand = new AsyncRelayCommand(SaveRolesAsync);
            NewCommand = new RelayCommand(NewUser);
        }

        private Task SaveRolesAsync()
        {
            // placeholder for save logic
            return Task.CompletedTask;
        }

        private void NewUser()
        {
            Users.Add(new UserItem { Email = "nieuw@biblio.local", FullName = "Nieuw" });
        }

        public class UserItem : ObservableObject
        {
            private string _email = string.Empty;
            public string Email { get => _email; set => SetProperty(ref _email, value); }
            private string _fullName = string.Empty;
            public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
            private bool _isAdmin;
            public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
            private bool _isStaff;
            public bool IsStaff { get => _isStaff; set => SetProperty(ref _isStaff, value); }
        }
    }
}
