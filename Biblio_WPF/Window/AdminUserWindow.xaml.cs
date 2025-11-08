using Biblio_WPF.ViewModels;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// zie commit bericht
    /// Interactielogica voor AdminUsersWindow.xaml
    /// </summary>
    public partial class AdminUsersWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminUsersWindow> _logger;
        private readonly SecurityViewModel _securityVm;

        // Beschikbaar voor XAML-bindings (RelativeSource AncestorType=Window)
        public SecurityViewModel SecurityVm => _securityVm;

        public ObservableCollection<AppUser> Users { get; } = new();

        private AppUser? _selectedUser;
        public AppUser? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser == value) return;
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                _ = LoadSelectedUserRolesAsync();
                // Update status van commando's
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }

        public IAsyncRelayCommand DeleteCommand { get; }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set { if (_isAdmin == value) return; _isAdmin = value; OnPropertyChanged(nameof(IsAdmin)); }
        }

        private bool _isStaff;
        public bool IsStaff
        {
            get => _isStaff;
            set { if (_isStaff == value) return; _isStaff = value; OnPropertyChanged(nameof(IsStaff)); }
        }

        private string? _newEmail;
        public string? NewEmail
        {
            get => _newEmail;
            set { _newEmail = value; OnPropertyChanged(nameof(NewEmail)); }
        }
        private string? _newPassword;
        public string? NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(nameof(NewPassword)); }
        }
        private string? _newFullName;
        public string? NewFullName
        {
            get => _newFullName;
            set { _newFullName = value; OnPropertyChanged(nameof(NewFullName)); }
        }
        private bool _newIsAdmin;
        public bool NewIsAdmin
        {
            get => _newIsAdmin;
            set { _newIsAdmin = value; OnPropertyChanged(nameof(NewIsAdmin)); }
        }
        private bool _newIsStaff = true;
        public bool NewIsStaff
        {
            get => _newIsStaff;
            set { _newIsStaff = value; OnPropertyChanged(nameof(NewIsStaff)); }
        }

        public AdminUsersWindow(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, SecurityViewModel securityVm)
        {
            InitializeComponent();

            _userManager = userManager;
            _roleManager = roleManager;
            _securityVm = securityVm;
            // logger via AppHost
            _logger = Biblio_WPF.App.AppHost.Services.GetRequiredService<ILogger<AdminUsersWindow>>();
            DataContext = this;

            DeleteCommand = new AsyncRelayCommand(async () => await DeleteSelectedUserAsync(), () => SelectedUser != null);

            Loaded += async (_, __) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            Users.Clear();
            foreach (var u in await _userManager.Users.ToListAsync())
                Users.Add(u);
        }

        private async Task LoadSelectedUserRolesAsync()
        {
            if (SelectedUser is null) { IsAdmin = IsStaff = false; return; }
            var roles = await _userManager.GetRolesAsync(SelectedUser);
            IsAdmin = roles.Contains("Admin");
            IsStaff = roles.Contains("Medewerker");
        }

        private async void OnSaveRoles(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is null) return;
            try
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!await _roleManager.RoleExistsAsync("Medewerker"))
                    await _roleManager.CreateAsync(new IdentityRole("Medewerker"));

                if (IsAdmin && !await _userManager.IsInRoleAsync(SelectedUser, "Admin"))
                    await _userManager.AddToRoleAsync(SelectedUser, "Admin");
                else if (!IsAdmin && await _userManager.IsInRoleAsync(SelectedUser, "Admin"))
                    await _userManager.RemoveFromRoleAsync(SelectedUser, "Admin");

                if (IsStaff && !await _userManager.IsInRoleAsync(SelectedUser, "Medewerker"))
                    await _userManager.AddToRoleAsync(SelectedUser, "Medewerker");
                else if (!IsStaff && await _userManager.IsInRoleAsync(SelectedUser, "Medewerker"))
                    await _userManager.RemoveFromRoleAsync(SelectedUser, "Medewerker");

                // Vernieuw en werk security VM bij indien nodig
                await LoadUsersAsync();
                await LoadSelectedUserRolesAsync();

                if (SelectedUser?.Email != null && SelectedUser.Email == _securityVm.CurrentEmail)
                {
                    var updatedRoles = await _userManager.GetRolesAsync(SelectedUser);
                    _securityVm.SetUser(SelectedUser.Email, updatedRoles.Contains("Admin"), updatedRoles.Contains("Medewerker"));
                }

                MessageBox.Show("Rollen opgeslagen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Opslaan mislukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnCreateUser(object sender, RoutedEventArgs e)
        {
            // Lees wachtwoord uit PasswordBox in de XAML via FindName
            var pwdBox = this.FindName("PwdBox") as PasswordBox;
            NewPassword = pwdBox?.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(NewPassword))
            {
                MessageBox.Show("E-mail en wachtwoord zijn verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var user = new AppUser { UserName = NewEmail.Trim(), Email = NewEmail.Trim(), FullName = NewFullName };
                var result = await _userManager.CreateAsync(user, NewPassword);
                if (!result.Succeeded)
                {
                    MessageBox.Show(string.Join("\n", result.Errors.Select(e => e.Description)), "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (NewIsAdmin)
                    await _userManager.AddToRoleAsync(user, "Admin");
                if (NewIsStaff)
                    await _userManager.AddToRoleAsync(user, "Medewerker");

                NewEmail = NewPassword = NewFullName = string.Empty;
                NewIsAdmin = false; NewIsStaff = true;
                await LoadUsersAsync();
                MessageBox.Show("Gebruiker aangemaakt.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Aanmaken mislukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnClearRoles(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is null) return;
            if (MessageBox.Show($"Verwijder alle rollen van {SelectedUser.Email}?", "Bevestig", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                if (await _userManager.IsInRoleAsync(SelectedUser, "Admin"))
                {
                    var res = await _userManager.RemoveFromRoleAsync(SelectedUser, "Admin");
                    if (!res.Succeeded) throw new Exception(string.Join("; ", res.Errors.Select(x => x.Description)));
                }
                if (await _userManager.IsInRoleAsync(SelectedUser, "Medewerker"))
                {
                    var res = await _userManager.RemoveFromRoleAsync(SelectedUser, "Medewerker");
                    if (!res.Succeeded) throw new Exception(string.Join("; ", res.Errors.Select(x => x.Description)));
                }
                await LoadUsersAsync();
                await LoadSelectedUserRolesAsync();
                _logger.LogInformation("Rollen verwijderd voor gebruiker {Email}", SelectedUser.Email);

                if (SelectedUser?.Email != null && SelectedUser.Email == _securityVm.CurrentEmail)
                {
                    _securityVm.SetUser(SelectedUser.Email, false, false);
                }

                MessageBox.Show("Rollen verwijderd.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwijderen rollen voor {Email}", SelectedUser.Email);
                MessageBox.Show($"Verwijderen mislukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteSelectedUserAsync()
        {
            if (SelectedUser is null) return;
            if (MessageBox.Show($"Verwijder gebruiker {SelectedUser.Email}?", "Bevestig", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                var email = SelectedUser.Email;
                var res = await _userManager.DeleteAsync(SelectedUser);
                if (!res.Succeeded) throw new Exception(string.Join(';', res.Errors.Select(x => x.Description)));

                await LoadUsersAsync();
                SelectedUser = null;
                _logger.LogInformation("Gebruiker verwijderd: {Email}", email);

                if (email != null && email == _securityVm.CurrentEmail)
                {
                    _securityVm.Reset();
                }

                MessageBox.Show("Gebruiker verwijderd.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwijderen gebruiker voor {Email}", SelectedUser?.Email);
                MessageBox.Show($"Verwijderen mislukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnToggleBlock(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is null) return;

            try
            {
                SelectedUser.IsBlocked = !SelectedUser.IsBlocked;
                var res = await _userManager.UpdateAsync(SelectedUser);
                if (!res.Succeeded) throw new Exception(string.Join(';', res.Errors.Select(x => x.Description)));

                await LoadUsersAsync();
                MessageBox.Show(SelectedUser.IsBlocked ? "Gebruiker geblokkeerd." : "Gebruiker geactiveerd.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij wijzigen blokkering: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
