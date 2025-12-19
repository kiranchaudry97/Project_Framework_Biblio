using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace Biblio_App.ViewModels
{
    public class SecurityViewModel : ObservableObject
    {
        private string? _currentEmail;
        public string? CurrentEmail
        {
            get => _currentEmail;
            private set => SetProperty(ref _currentEmail, value);
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            private set => SetProperty(ref _isAdmin, value);
        }

        private bool _isStaff;
        public bool IsStaff
        {
            get => _isStaff;
            private set => SetProperty(ref _isStaff, value);
        }

        private string? _fullName;
        public string? FullName
        {
            get => _fullName;
            private set => SetProperty(ref _fullName, value);
        }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(CurrentEmail);

        public void SetUser(string? email, bool isAdmin = false, bool isStaff = false, string? fullName = null)
        {
            CurrentEmail = email;
            IsAdmin = isAdmin;
            IsStaff = isStaff;
            FullName = fullName;
            OnPropertyChanged(nameof(IsAuthenticated));

            try
            {
                Debug.WriteLine($"SecurityViewModel.SetUser called: Email='{email}', IsAdmin={isAdmin}, IsStaff={isStaff}, FullName='{fullName}'");
            }
            catch { }
        }

        public void Reset()
        {
            CurrentEmail = null;
            IsAdmin = false;
            IsStaff = false;
            FullName = null;
            OnPropertyChanged(nameof(IsAuthenticated));

            try
            {
                Debug.WriteLine("SecurityViewModel.Reset called: cleared authentication state");
            }
            catch { }
        }
    }
}
