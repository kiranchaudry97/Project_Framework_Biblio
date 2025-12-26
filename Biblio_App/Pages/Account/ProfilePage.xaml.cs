using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;
using Microsoft.Maui.Storage;
using Biblio_App.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Biblio_Models.Entiteiten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel;

namespace Biblio_App.Pages.Account
{
    public partial class ProfilePage : ContentPage, ILocalizable
    {
        private readonly SecurityViewModel _security;
        private ILanguageService? _languageService;

        public ProfilePage() : this(App.Current?.Handler?.MauiContext?.Services?.GetService<SecurityViewModel>() ?? new SecurityViewModel())
        {
        }

        public ProfilePage(SecurityViewModel security)
        {
            InitializeComponent();
            _security = security;
            BindingContext = new ProfilePageViewModel(security);

            try
            {
                try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                try { Shell.SetFlyoutBehavior(this, FlyoutBehavior.Flyout); } catch { }
                try { NavigationPage.SetHasBackButton(this, false); } catch { }
            }
            catch { }

            try { _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }
        }

        private async void OnLogoutClicked(object sender, System.EventArgs e)
        {
            _security.Reset();
            Preferences.Default.Remove("CurrentEmail");
            Preferences.Default.Remove("IsAdmin");
            Preferences.Default.Remove("IsStaff");
            try { await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync("api_token", string.Empty); } catch { }
            try { await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync("refresh_token", string.Empty); } catch { }
            await Shell.Current.DisplayAlert("Logout", "Je bent afgemeld.", "OK");

            // Ensure the flyout is closed and navigate to the LoginPage as a new root
            try { if (Shell.Current != null) Shell.Current.FlyoutIsPresented = false; } catch { }
            try
            {
                await Shell.Current.GoToAsync($"//{nameof(Pages.Account.LoginPage)}", animate: false);
            }
            catch
            {
                // Fallback: navigate to root if named route fails
                try { await Shell.Current.GoToAsync("//"); } catch { }
            }
        }

        public void UpdateLocalizedStrings()
        {
            try
            {
                var header = "Profiel"; // could be localized via resources if present
                try { if (TitleLabel != null) TitleLabel.Text = header; } catch { }
                try { if (HeaderLabel != null) HeaderLabel.Text = header; } catch { }
            }
            catch { }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Keep default flyout behavior so this page shows the same header/hamburger as other pages

                if (_languageService != null)
                {
                    _languageService.LanguageChanged += LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                // Keep default flyout behavior

                if (_languageService != null)
                {
                    _languageService.LanguageChanged -= LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateLocalizedStrings();
                });
            }
            catch { }
        }

        // New: save profile handler — updates FullName via local DB when possible (uses registered DbContext)
        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            // Clear previous errors/status
            try { FullNameError.Text = string.Empty; } catch { }
            try { StatusLabel.Text = string.Empty; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Gray; } catch { }

            var fullName = FullNameEntry?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                try { FullNameError.Text = "Volledige naam is verplicht."; } catch { }
                try { StatusLabel.Text = "Er zijn validatiefouten."; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red; } catch { }
                return;
            }
            if (fullName.Length < 3)
            {
                try { FullNameError.Text = "Minimaal 3 tekens."; } catch { }
                try { StatusLabel.Text = "Er zijn validatiefouten."; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red; } catch { }
                return;
            }

            // Try to update remote via API if token-guarded client is available, otherwise attempt local DbContext update via registered factory
            try
            {
                var services = App.Current?.Handler?.MauiContext?.Services;
                if (services != null)
                {
                    // Try to update via ASP.NET Identity UserManager when running alongside server (Windows desktop with services configured)
                    var userMgr = services.GetService<UserManager<AppUser>>();
                    if (userMgr != null && !string.IsNullOrWhiteSpace(_security.CurrentEmail))
                    {
                        var user = await userMgr.FindByEmailAsync(_security.CurrentEmail);
                        if (user != null)
                        {
                            user.FullName = fullName;
                            var res = await userMgr.UpdateAsync(user);
                            if (res.Succeeded)
                            {
                                _security.SetUser(_security.CurrentEmail, _security.IsAdmin, _security.IsStaff, fullName);
                                try { StatusLabel.Text = "Profiel succesvol opgeslagen."; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Green; } catch { }
                                return;
                            }
                            else
                            {
                                var msgs = string.Join(';', res.Errors.Select(err => err.Description));
                                try { StatusLabel.Text = $"Fout bij opslaan: {msgs}"; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red; } catch { }
                                return;
                            }
                        }
                    }

                    // Fallback: try updating profile in local BiblioDbContext if available (e.g., local SQLite)
                    var dbFactory = services.GetService<Microsoft.EntityFrameworkCore.IDbContextFactory<Biblio_Models.Data.BiblioDbContext>>();
                    if (dbFactory != null && !string.IsNullOrWhiteSpace(_security.CurrentEmail))
                    {
                        using var db = dbFactory.CreateDbContext();
                        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == _security.CurrentEmail);
                        if (user != null)
                        {
                            user.FullName = fullName;
                            db.Users.Update(user);
                            await db.SaveChangesAsync();
                            _security.SetUser(_security.CurrentEmail, _security.IsAdmin, _security.IsStaff, fullName);
                            try { StatusLabel.Text = "Profiel succesvol opgeslagen (lokaal)."; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Green; } catch { }
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try { StatusLabel.Text = "Fout bij opslaan. Probeer het opnieuw."; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red; } catch { }
                try { System.Diagnostics.Debug.WriteLine($"Profile save error: {ex}"); } catch { }
            }

            // If we reach here, we couldn't save profile
            try { StatusLabel.Text = "Kon profiel niet opslaan."; StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red; } catch { }
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            // Clear previous status
            try { PasswordStatusLabel.Text = string.Empty; PasswordStatusLabel.IsVisible = false; } catch { }

            // Validate inputs
            var currentPwd = CurrentPasswordEntry?.Text?.Trim() ?? string.Empty;
            var newPwd = NewPasswordEntry?.Text?.Trim() ?? string.Empty;
            var confirmPwd = ConfirmPasswordEntry?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(currentPwd))
            {
                try { PasswordStatusLabel.Text = "Huidig wachtwoord is verplicht."; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                return;
            }

            if (string.IsNullOrWhiteSpace(newPwd))
            {
                try { PasswordStatusLabel.Text = "Nieuw wachtwoord is verplicht."; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                return;
            }

            if (newPwd.Length < 6)
            {
                try { PasswordStatusLabel.Text = "Nieuw wachtwoord moet minimaal 6 tekens zijn."; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                return;
            }

            if (newPwd != confirmPwd)
            {
                try { PasswordStatusLabel.Text = "Nieuwe wachtwoorden komen niet overeen."; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                return;
            }

            // Try to change password via UserManager
            try
            {
                var services = App.Current?.Handler?.MauiContext?.Services;
                if (services != null)
                {
                    var userMgr = services.GetService<UserManager<AppUser>>();
                    if (userMgr != null && !string.IsNullOrWhiteSpace(_security.CurrentEmail))
                    {
                        var user = await userMgr.FindByEmailAsync(_security.CurrentEmail);
                        if (user != null)
                        {
                            var res = await userMgr.ChangePasswordAsync(user, currentPwd, newPwd);
                            if (res.Succeeded)
                            {
                                try { PasswordStatusLabel.Text = "Wachtwoord succesvol gewijzigd!"; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Green; PasswordStatusLabel.IsVisible = true; } catch { }
                                
                                // Clear password fields
                                try { CurrentPasswordEntry.Text = string.Empty; } catch { }
                                try { NewPasswordEntry.Text = string.Empty; } catch { }
                                try { ConfirmPasswordEntry.Text = string.Empty; } catch { }
                                
                                await DisplayAlert("Succes", "Wachtwoord succesvol gewijzigd!", "OK");
                                return;
                            }
                            else
                            {
                                var msgs = string.Join(", ", res.Errors.Select(err => err.Description));
                                try { PasswordStatusLabel.Text = $"Fout: {msgs}"; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                                await DisplayAlert("Fout", msgs, "OK");
                                return;
                            }
                        }
                        else
                        {
                            try { PasswordStatusLabel.Text = "Gebruiker niet gevonden."; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try { PasswordStatusLabel.Text = "Fout bij wijzigen wachtwoord."; PasswordStatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Crimson; PasswordStatusLabel.IsVisible = true; } catch { }
                try { System.Diagnostics.Debug.WriteLine($"Password change error: {ex}"); } catch { }
                await DisplayAlert("Fout", "Kon wachtwoord niet wijzigen. Probeer het opnieuw.", "OK");
            }
        }

        private void OnShowPasswordToggled(object sender, ToggledEventArgs e)
        {
            try
            {
                var showPassword = e.Value;
                if (CurrentPasswordEntry != null) CurrentPasswordEntry.IsPassword = !showPassword;
                if (NewPasswordEntry != null) NewPasswordEntry.IsPassword = !showPassword;
                if (ConfirmPasswordEntry != null) ConfirmPasswordEntry.IsPassword = !showPassword;
            }
            catch { }
        }
    }
}
