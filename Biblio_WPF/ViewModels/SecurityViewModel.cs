// zie commit bericht //
using System.ComponentModel;

namespace Biblio_WPF.ViewModels
{
    public class SecurityViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private string? _currentEmail;
        public string? CurrentEmail { get => _currentEmail; private set { _currentEmail = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEmail))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthenticated))); } }

        private bool _isAdmin;
        public bool IsAdmin { get => _isAdmin; private set { _isAdmin = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAdmin))); } }

        private bool _isStaff;
        public bool IsStaff { get => _isStaff; private set { _isStaff = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStaff))); } }

        private string? _fullName;
        public string? FullName { get => _fullName; private set { _fullName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName))); } }

        // New: berkende eigenschappen om een geauthenticeerde gebruiker te controleren en aan te duiden
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(CurrentEmail);

        public void SetUser(string? email, bool isAdmin, bool isStaff, string? fullName = null)
        {
            CurrentEmail = email;
            IsAdmin = isAdmin;
            IsStaff = isStaff;
            FullName = fullName;
            // andere afhankelijke eigenschappen op de hoogte stellen
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthenticated)));
        }

        public void Reset()
        {
            CurrentEmail = null;
            IsAdmin = false;
            IsStaff = false;
            FullName = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthenticated)));
        }
    }
}
