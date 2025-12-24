using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using System.Threading.Tasks;

namespace Biblio_App.Pages.Account
{
    public partial class LoginPage : ContentPage
    {
        private readonly SecurityViewModel? _securityViewModel;

        public LoginPage()
        {
            InitializeComponent();

#if DEBUG
            DevPanel.IsVisible = true;
#else
            DevPanel.IsVisible = false;
#endif

            try
            {
                // Ensure login page does not show flyout or back button
                try
                {
                    Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
                    NavigationPage.SetHasBackButton(this, false);
                    // Also explicitly hide Shell's back button for Shell scenarios
                    try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                    // Optionally ensure the navigation bar is visible (keeps title area) but without back button
                    try { Shell.SetNavBarIsVisible(this, true); } catch { }
                }
                catch { }

                // Resolve SecurityViewModel once to avoid service resolution overhead on every login
                try
                {
                    var services = App.Current?.Handler?.MauiContext?.Services;
                    if (services != null) _securityViewModel = services.GetService<SecurityViewModel>();
                }
                catch { }

                try
                {
                    if (_securityViewModel == null)
                    {
                        var svc = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                        if (svc != null) _securityViewModel = svc.GetService<SecurityViewModel>();
                    }
                }
                catch { }

                // Restore 'Remember me' state
                if (Preferences.Default.ContainsKey("RememberMe"))
                {
                    var rem = Preferences.Default.Get("RememberMe", "0");
                    RememberCheck.IsChecked = rem == "1";
                }

                // If email was remembered, prefill
                if (Preferences.Default.ContainsKey("RememberedEmail"))
                {
                    var e = Preferences.Default.Get("RememberedEmail", string.Empty);
                    if (!string.IsNullOrWhiteSpace(e)) EmailEntry.Text = e;
                }
            }
            catch { }
        }

        private async void OnLoginClicked(object sender, System.EventArgs e)
        {
            // Show busy indicator and disable the login button so user sees immediate feedback
            try
            {
                BusyIndicator.IsVisible = true;
                BusyIndicator.IsRunning = true;
            }
            catch { }

            try
            {
                try
                {
                    var email = EmailEntry?.Text?.Trim() ?? string.Empty;
                    var password = PasswordEntry?.Text ?? string.Empty;

                    // allow UI to update before doing any heavier work
                    await Task.Yield();

                    System.Diagnostics.Debug.WriteLine($"OnLoginClicked: invoked. Email='{email}', PasswordLength={password?.Length}");

                    // Defensive checks: ensure XAML controls exist
                    if (EmailEntry == null || PasswordEntry == null)
                    {
                        System.Diagnostics.Debug.WriteLine("OnLoginClicked: EmailEntry or PasswordEntry is null - UI may not beinitialized.");
                        await DisplayAlert("Login", "Login UI niet beschikbaar. Probeer het opnieuw.", "OK");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    {
                        System.Diagnostics.Debug.WriteLine("OnLoginClicked: missing email or password");
                        await DisplayAlert("Login", "Vul e-mail en wachtwoord in.", "OK");
                        return;
                    }

                    // Local-only authentication: only allow two development accounts
                    bool success = false;
                    bool isAdmin = false;
                    bool isStaff = false;
                    string? fullName = null;

                    if (string.Equals(email, "admin@biblio.local", StringComparison.OrdinalIgnoreCase) && password == "Admin1234?")
                    {
                        success = true;
                        isAdmin = true;
                        isStaff = true; // also mark staff true for admin
                        fullName = "Administrator";
                    }
                    else if (string.Equals(email, "medewerker@biblio.local", StringComparison.OrdinalIgnoreCase) && password == "test1234?")
                    {
                        success = true;
                        isAdmin = false; // medewerker should not be admin
                        isStaff = true;
                        fullName = "Medewerker";
                    }

                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"OnLoginClicked: authentication succeeded for '{email}'. isAdmin={isAdmin}, isStaff={isStaff}");

                        // Ensure flyout is enabled now that the user is authenticated
                        try { AppShell.Instance?.EnsureLoginFlyoutHidden(false); } catch { }

                        try
                        {
                            // Use the cached SecurityViewModel resolved in the constructor
                            _securityViewModel?.SetUser(email, isAdmin: isAdmin, isStaff: isStaff, fullName: fullName);
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"OnLoginClicked: setting SecurityViewModel failed: {ex}"); }

                        try { Preferences.Default.Set("CurrentEmail", email); } catch { }
                        try { Preferences.Default.Set("IsAdmin", isAdmin ? "1" : "0"); } catch { }
                        try { Preferences.Default.Set("IsStaff", isStaff ? "1" : "0"); } catch { }

                        // Remember me
                        try
                        {
                            var remember = false;
                            try { remember = RememberCheck?.IsChecked ?? false; } catch { remember = false; }

                            Preferences.Default.Set("RememberMe", remember ? "1" : "0");
                            if (remember)
                            {
                                Preferences.Default.Set("RememberedEmail", email);
                            }
                            else
                            {
                                try { Preferences.Default.Remove("RememberedEmail"); } catch { }
                            }
                        }
                        catch { }

                        // Force UI update of flyout/profile texts so user immediately sees logged-in state
                        try
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try { AppShell.Instance?.UpdateLocalizedFlyoutTitles(); } catch { }
                            });
                        }
                        catch { }

                        // Log current security state for debugging
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"OnLoginClicked: SecurityViewModel state -> CurrentEmail='{_securityViewModel?.CurrentEmail}', IsAdmin={_securityViewModel?.IsAdmin}, IsStaff={_securityViewModel?.IsStaff}, IsAuthenticated={_securityViewModel?.IsAuthenticated}");
                        }
                        catch { }

                        // Log main page / shell state before navigation
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"OnLoginClicked: MainPage={(App.Current?.Windows?.Count > 0 ? App.Current.Windows[0]?.Page?.GetType()?.FullName : "null")}, ShellCurrent={(Shell.Current == null ? "null" : Shell.Current.GetType().FullName)}");
                        }
                        catch { }

                        // Navigate to BoekenPagina (books) after successful login on UI thread
                        try
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                try
                                {
                                    if (Shell.Current != null)
                                    {
                                        try
                                        {
                                            // Prefer navigating to the FlyoutItem root so Shell shows the hamburger
                                            await Shell.Current.GoToAsync("//BoekenShell");
                                        }
                                        catch (Exception absEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"OnLoginClicked: absolute navigation failed: {absEx}");
                                            // Some platforms/Shell states disallow absolute navigation to global routes when it's the only page.
                                            // Fallback to a relative navigation to the page route or push page directly.
                                            try
                                            {
                                                await Shell.Current.GoToAsync(nameof(Pages.BoekenPagina));
                                                System.Diagnostics.Debug.WriteLine("OnLoginClicked: relative navigation to BoekenPagina attempted as fallback");
                                            }
                                            catch (Exception relEx)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"OnLoginClicked: relative navigation also failed: {relEx}");
                                                try
                                                {
                                                    System.Diagnostics.Debug.WriteLine("OnLoginClicked: both absolute and relative navigation failed; cannot push directly because page constructor may require dependencies.");
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    else if (App.Current?.Windows?.Count > 0 && App.Current.Windows[0]?.Page is AppShell appShell)
                                    {
                                        try { await appShell.GoToAsync("//BoekenShell"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"OnLoginClicked: appshell.GoToAsync failed: {ex}"); }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("OnLoginClicked: No Shell available to navigate.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"OnLoginClicked: navigation inner failed: {ex}");
                                }
                            });

                            System.Diagnostics.Debug.WriteLine("OnLoginClicked: navigation to BoekenPagina requested");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"OnLoginClicked: navigation failed: {ex}");
                            try { if (App.Current?.Windows?.Count > 0 && App.Current.Windows[0]?.Page != null) await Shell.Current.GoToAsync("//" + nameof(Pages.MainPage)); } catch (Exception ex2) { System.Diagnostics.Debug.WriteLine($"OnLoginClicked: fallback navigation failed: {ex2}"); }
                        }

                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"OnLoginClicked: authentication failed for '{email}'");
                    await DisplayAlert("Login", "Inloggen mislukt. Alleen admin of medewerker accounts zijn toegestaan.", "OK");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OnLoginClicked error: {ex}");
                    await DisplayAlert("Login", "Fout bij inloggen. Probeer het later opnieuw.", "OK");
                }
            }
            finally
            {
                try
                {
                    BusyIndicator.IsRunning = false;
                    BusyIndicator.IsVisible = false;
                }
                catch { }

                try { LoginButton.IsEnabled = true; } catch { }
            }
        }

        private void OnAutofillAdminClicked(object sender, System.EventArgs e)
        {
            EmailEntry.Text = "admin@biblio.local";
            PasswordEntry.Text = "Admin1234?";
        }

        private void OnAutofillMedewerkerClicked(object sender, System.EventArgs e)
        {
            EmailEntry.Text = "medewerker@biblio.local";
            PasswordEntry.Text = "test1234?";
        }

        private void OnShowPasswordToggled(object sender, ToggledEventArgs e)
        {
            try
            {
                PasswordEntry.IsPassword = !e.Value;
            }
            catch { }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Ensure the flyout is hidden while on the login page
                try { AppShell.Instance?.EnsureLoginFlyoutHidden(true); } catch { }
            }
            catch { }
        }
    }
}
