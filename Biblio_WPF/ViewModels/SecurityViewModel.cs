// zie commit bericht //
using System.ComponentModel;

namespace Biblio_WPF.ViewModels
{
    public class SecurityViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private string? _currentEmail;
        public string? CurrentEmail { get => _currentEmail; private set { _currentEmail = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEmail))); } }

        private bool _isAdmin;
        public bool IsAdmin { get => _isAdmin; private set { _isAdmin = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAdmin))); } }

        private bool _isStaff;
        public bool IsStaff { get => _isStaff; private set { _isStaff = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStaff))); } }

        public void SetUser(string? email, bool isAdmin, bool isStaff)
        {
            CurrentEmail = email;
            IsAdmin = isAdmin;
            IsStaff = isStaff;
        }

        public void Reset()
        {
            CurrentEmail = null;
            IsAdmin = false;
            IsStaff = false;
        }
    }
}
