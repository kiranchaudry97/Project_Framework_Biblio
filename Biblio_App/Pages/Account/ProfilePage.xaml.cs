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

        public ProfilePage(SecurityViewModel security)
        {
            InitializeComponent();
            _security = security;
            BindingContext = new ProfilePageViewModel(security);

            try { _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }
        }

        private async void OnLogoutClicked(object sender, System.EventArgs e)
        {
            _security.Reset();
            Preferences.Default.Remove("CurrentEmail");
            Preferences.Default.Remove("IsAdmin");
            Preferences.Default.Remove("IsStaff");
            await Shell.Current.DisplayAlert("Logout", "Je bent afgemeld.", "OK");
            await Shell.Current.GoToAsync("//Home");
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

        private void OnChangePasswordClicked(object sender, EventArgs e)
        {
            // Navigate to the change password view — use Shell navigation to the web Identity change page or app page if present
            try
            {
                // First try local navigation to ChangePassword page in the app (not present by default)
                var routes = Shell.Current?.CurrentItem?.Items?.Select(i => i.Route).ToList();
                // Fallback to opening external web page to change password via the site (if configured)
                var cfg = App.Current?.Handler?.MauiContext?.Services?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                var webBase = cfg?[("ApiBaseAddress")] ?? cfg?.GetSection("Api")?["BaseAddress"];
                if (!string.IsNullOrWhiteSpace(webBase))
                {
                    var url = webBase.TrimEnd('/') + "/Identity/Account/Manage";
                    try { Launcher.OpenAsync(url); } catch { }
                }
            }
            catch { }
        }
    }
}
