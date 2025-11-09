using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// zie commit bericht
    /// </summary>
    public partial class RegisterWindow : Page
    {
        public RegisterWindow()
        {
            InitializeComponent();
            Loaded += RegisterWindow_Loaded;
        }

        private void RegisterWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Configure visibility/enabled state for Admin checkbox based on current user
            try
            {
                var services = Biblio_WPF.App.AppHost?.Services;
                var security = services?.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
                var adminCheck = this.FindName("IsAdminCheck") as CheckBox;
                if (adminCheck != null)
                {
                    var canAssignAdmin = security != null && security.IsAdmin && string.Equals(security.CurrentEmail, "admin@biblio.local", System.StringComparison.OrdinalIgnoreCase);
                    // keep checkbox visible but enable only for main admin
                    adminCheck.Visibility = Visibility.Visible;
                    adminCheck.IsEnabled = canAssignAdmin;
                    if (!canAssignAdmin)
                    {
                        adminCheck.IsChecked = false;
                        adminCheck.ToolTip = "Alleen hoofdbeheerder kan admin-rechten toewijzen.";
                    }
                    else
                    {
                        adminCheck.ToolTip = null;
                    }
                }
            }
            catch
            {
                // ignore errors in UI setup
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }

        private async void OnRegister(object sender, RoutedEventArgs e)
        {
            var email = (this.FindName("EmailBox") as TextBox)?.Text?.Trim();
            var fullName = (this.FindName("FullNameBox") as TextBox)?.Text?.Trim();
            var pwd = (this.FindName("PwdBox") as PasswordBox)?.Password;
            var isStaff = (this.FindName("IsStaffCheck") as CheckBox)?.IsChecked == true;
            var isAdmin = (this.FindName("IsAdminCheck") as CheckBox)?.IsChecked == true;
            var resultText = this.FindName("ResultText") as TextBlock;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
            {
                MessageBox.Show("E-mail en wachtwoord zijn verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure the email uses the biblio.local domain.
            // If user entered only the local part (no '@'), append '@biblio.local'.
            // If user provided a domain different from biblio.local, show validation error.
            email = email!.ToLowerInvariant();
            if (!email.Contains("@"))
            {
                email = $"{email}@biblio.local";
            }
            else
            {
                var parts = email.Split('@');
                if (parts.Length != 2 || !string.Equals(parts[1], "biblio.local", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("E-mailadres moet eindigen op '@biblio.local'.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var services = Biblio_WPF.App.AppHost?.Services;
            if (services == null) return;

            var userManager = services.GetService<UserManager<Biblio_Models.Entiteiten.AppUser>>();
            var roleManager = services.GetService<RoleManager<IdentityRole>>();
            var security = services.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
            if (userManager == null || roleManager == null)
            {
                MessageBox.Show("User manager niet beschikbaar.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Only allow assigning Admin role when current user is the main admin (admin@biblio.local)
            var canAssignAdmin = security != null && security.IsAdmin && string.Equals(security.CurrentEmail, "admin@biblio.local", System.StringComparison.OrdinalIgnoreCase);
            if (isAdmin && !canAssignAdmin)
            {
                // If the caller is not the main admin we ignore the admin flag and inform.
                isAdmin = false;
                if (resultText != null) resultText.Text = "Alleen hoofdbeheerder kan admin-rechten toewijzen. De gebruiker is zonder admin-recht aangemaakt.";
            }

            try
            {
                var existing = await userManager.FindByEmailAsync(email!);
                if (existing != null)
                {
                    if (resultText != null) resultText.Text = "E-mail is al in gebruik.";
                    return;
                }

                var user = new Biblio_Models.Entiteiten.AppUser { UserName = email, Email = email, FullName = fullName };
                var res = await userManager.CreateAsync(user, pwd!);
                if (!res.Succeeded)
                {
                    if (resultText != null) resultText.Text = string.Join('\n', res.Errors.Select(x => x.Description));
                    return;
                }

                // Zorg dat rollen bestaan
                if (!await roleManager.RoleExistsAsync("Medewerker"))
                    await roleManager.CreateAsync(new IdentityRole("Medewerker"));
                if (!await roleManager.RoleExistsAsync("Admin"))
                    await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (isStaff)
                    await userManager.AddToRoleAsync(user, "Medewerker");
                if (isAdmin)
                    await userManager.AddToRoleAsync(user, "Admin");

                if (resultText != null && string.IsNullOrWhiteSpace(resultText.Text)) resultText.Text = "Gebruiker aangemaakt.";
                // Sluit venster kort na succes
                await Task.Delay(600);
                var wnd = System.Windows.Window.GetWindow(this);
                wnd?.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fout bij registreren: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Toon/Verberg wachtwoord handlers
        public void OnShowPasswordChecked(object sender, RoutedEventArgs e)
        {
            var pwdText = this.FindName("PwdTextBox") as TextBox;
            var pwdBox = this.FindName("PwdBox") as PasswordBox;
            if (pwdText != null && pwdBox != null)
            {
                pwdText.Text = pwdBox.Password;
                pwdText.Visibility = Visibility.Visible;
                pwdBox.Visibility = Visibility.Collapsed;
            }
        }

        public void OnShowPasswordUnchecked(object sender, RoutedEventArgs e)
        {
            var pwdText = this.FindName("PwdTextBox") as TextBox;
            var pwdBox = this.FindName("PwdBox") as PasswordBox;
            if (pwdText != null && pwdBox != null)
            {
                pwdBox.Password = pwdText.Text;
                pwdText.Visibility = Visibility.Collapsed;
                pwdBox.Visibility = Visibility.Visible;
            }
        }
    }
}
