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
            // Configureer zichtbaarheid/enabled status voor role-checkboxes op basis van de huidige gebruiker
            try
            {
                var services = Biblio_WPF.App.AppHost?.Services;
                var security = services?.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();

                var canAssignAdmin = security != null && security.IsAdmin && string.Equals(security.CurrentEmail, "admin@biblio.local", System.StringComparison.OrdinalIgnoreCase);

                var adminCheck = this.FindName("IsAdminCheck") as CheckBox;
                if (adminCheck != null)
                {
                    // Verberg de Admin-checkbox voor niet-hoofdbeheerders
                    adminCheck.Visibility = canAssignAdmin ? Visibility.Visible : Visibility.Collapsed;
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

                var staffCheck = this.FindName("IsStaffCheck") as CheckBox;
                if (staffCheck != null)
                {
                    // Verberg de Medewerker-checkbox voor niet-hoofdbeheerders
                    staffCheck.Visibility = canAssignAdmin ? Visibility.Visible : Visibility.Collapsed;
                    if (!canAssignAdmin)
                    {
                        staffCheck.IsChecked = false;
                        staffCheck.ToolTip = "Alleen hoofdbeheerder kan rollen toewijzen.";
                    }
                    else
                    {
                        staffCheck.ToolTip = null;
                    }
                }
            }
            catch
            {
                // Negeer fouten tijdens het opzetten van de UI
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
            // Lees wachtwoord uit de controls; geef de voorkeur aan PasswordBox maar val terug op PwdTextBox indien nodig
            var pwdBox = this.FindName("PwdBox") as PasswordBox;
            var pwdTextBox = this.FindName("PwdTextBox") as TextBox;
            var pwd = pwdBox?.Password;
            if (string.IsNullOrEmpty(pwd) && pwdTextBox != null)
                pwd = pwdTextBox.Text;

            var isStaff = (this.FindName("IsStaffCheck") as CheckBox)?.IsChecked == true;
            var isAdmin = (this.FindName("IsAdminCheck") as CheckBox)?.IsChecked == true;
            var resultText = this.FindName("ResultText") as TextBlock;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
            {
                MessageBox.Show("E-mail en wachtwoord zijn verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Zorg dat het e-mailadres het domein biblio.local gebruikt.
            // Als alleen het lokale deel is ingevoerd (zonder '@'), voeg dan '@biblio.local' toe.
            // Als een ander domein is opgegeven, toon validatiefout.
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

            // Alleen hoofdbeheerder (admin@biblio.local) mag de Admin-rol toewijzen
            var canAssignAdmin = security != null && security.IsAdmin && string.Equals(security.CurrentEmail, "admin@biblio.local", System.StringComparison.OrdinalIgnoreCase);
            if (isAdmin && !canAssignAdmin)
            {
                // Als de aanroeper geen hoofdbeheerder is, negeren we de admin-vlag en informeren we de gebruiker
                isAdmin = false;
                if (resultText != null) resultText.Text = "Alleen hoofdbeheerder kan admin-rechten toewijzen. De gebruiker is zonder admin-recht aangemaakt.";
            }

            // Zorg dat alleen hoofdbeheerder de rol 'Medewerker' kan toewijzen (dubbele controle)
            if (isStaff && !canAssignAdmin)
            {
                isStaff = false;
                if (resultText != null && string.IsNullOrWhiteSpace(resultText.Text))
                    resultText.Text = "Alleen hoofdbeheerder kan rollen toewijzen. De gebruiker is zonder extra rollen aangemaakt.";
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
