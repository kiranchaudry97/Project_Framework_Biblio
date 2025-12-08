using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;

namespace Biblio_App
{
    public partial class AppShell : Shell
    {
        public static AppShell? Instance { get; private set; }

        public ICommand LoginCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        public AppShell()
        {
            Instance = this;

            InitializeComponent();

            // Register routes so Shell navigation can use route strings
            Routing.RegisterRoute(nameof(Pages.BoekenPagina), typeof(Pages.BoekenPagina));
            Routing.RegisterRoute(nameof(Pages.LedenPagina), typeof(Pages.LedenPagina));
            Routing.RegisterRoute(nameof(Pages.UitleningenPagina), typeof(Pages.UitleningenPagina));
            Routing.RegisterRoute(nameof(Pages.CategorieenPagina), typeof(Pages.CategorieenPagina));
            Routing.RegisterRoute(nameof(Pages.Account.ProfilePage), typeof(Pages.Account.ProfilePage));
            Routing.RegisterRoute(nameof(Pages.MainPage), typeof(Pages.MainPage));
            Routing.RegisterRoute(nameof(Pages.InstellingenPagina), typeof(Pages.InstellingenPagina));
            Routing.RegisterRoute(nameof(Pages.BoekDetailsPage), typeof(Pages.BoekDetailsPage));
            Routing.RegisterRoute(nameof(Pages.LidDetailsPage), typeof(Pages.LidDetailsPage));

            // Use instance navigation methods to avoid depending on Shell.Current during construction
            LoginCommand = new Command(async () => await this.GoToAsync(nameof(Pages.Account.LoginPage)));
            OpenSettingsCommand = new Command(async () => await this.GoToAsync(nameof(Pages.InstellingenPagina)));

            // Initialize language picker
            try
            {
                LanguagePicker.ItemsSource = new List<string> { "nl", "en" };

                // Prevent SelectedIndex change event from firing during initialization.
                try
                {
                    LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
                }
                catch { /* harmless if not yet attached */ }

                var cur = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                LanguagePicker.SelectedIndex = cur == "en" ? 1 : 0;

                // Re-attach after initial set so runtime changes still trigger handler.
                LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"AppShell initialization error: {ex}");
            }

            UpdateThemeIcon();

            // Runtime validation to catch missing pages / controls early
            try
            {
                ValidatePages();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ValidatePages error: {ex}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                var isAdmin = false;
                var isStaff = false;

                var email = Preferences.Default.ContainsKey("CurrentEmail") ? Preferences.Default.Get("CurrentEmail", string.Empty) : null;

                // If you store roles as preferences, read them here
                if (Preferences.Default.ContainsKey("IsAdmin")) isAdmin = Preferences.Default.Get("IsAdmin", false);
                if (Preferences.Default.ContainsKey("IsStaff")) isStaff = Preferences.Default.Get("IsStaff", false);

                QuickNewBookButton.IsVisible = isAdmin || isStaff;
                QuickNewMemberButton.IsVisible = isAdmin || isStaff;

                if (!string.IsNullOrEmpty(email))
                {
                    ProfileMenuItem.Text = $"Profiel ({email})";
                    LogoutMenuItem.IsEnabled = true;
                }
                else
                {
                    ProfileMenuItem.Text = "Profiel";
                    LogoutMenuItem.IsEnabled = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnAppearing error: {ex}");
            }
        }

        private void UpdateThemeIcon()
        {
            try
            {
                var isDark = Application.Current?.UserAppTheme == AppTheme.Dark || Application.Current?.RequestedTheme == AppTheme.Dark;
                var img = new Image { Source = isDark ? "moon.svg" : "sun.svg", WidthRequest = 18, HeightRequest = 18 };
                // Use Image as icon inside the Button by setting ImageSource
                ThemeToggleButton.ImageSource = img.Source;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"UpdateThemeIcon error: {ex}");
            }
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            try
            {
                var current = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
                var next = (current == AppTheme.Dark) ? AppTheme.Light : AppTheme.Dark;
                Application.Current.UserAppTheme = next;
                try { Preferences.Default.Set("biblio-theme", next == AppTheme.Dark ? "dark" : "light"); } catch { }
                UpdateThemeIcon();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnThemeToggleClicked error: {ex}");
            }
        }

        private async void OnLanguageChanged(object sender, EventArgs e)
        {
            try
            {
                if (LanguagePicker.SelectedIndex < 0) return;
                var code = LanguagePicker.Items[LanguagePicker.SelectedIndex];
                var culture = new CultureInfo(code);

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                try { Preferences.Default.Set("biblio-culture", code); } catch { }

                // Use the instance DisplayAlert to avoid depending on Shell.Current being set
                await DisplayAlert("Taal", $"Taal ingesteld op: {code}. Herstart de app om alle teksten bij te werken.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnLanguageChanged error: {ex}");
            }
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            try
            {
                var email = Preferences.Default.ContainsKey("CurrentEmail") ? Preferences.Default.Get("CurrentEmail", string.Empty) : null;
                if (string.IsNullOrEmpty(email))
                {
                    await DisplayAlert("Profiel", "Geen ingelogde gebruiker. Log in via de web-app of Identity UI.", "OK");
                    return;
                }

                await this.GoToAsync(nameof(Pages.Account.ProfilePage));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnProfileClicked error: {ex}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                Preferences.Default.Remove("CurrentEmail");
                Preferences.Default.Remove("IsAdmin");
                Preferences.Default.Remove("IsStaff");

                var sec = App.Current?.Handler?.MauiContext?.Services?.GetService<ViewModels.SecurityViewModel>();
                sec?.Reset();

                await DisplayAlert("Logout", "Je bent afgemeld.", "OK");
                await this.GoToAsync("//");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"OnLogoutClicked error: {ex}");
            }
        }

        private async void QuickNewBook_Clicked(object sender, EventArgs e)
        {
            try
            {
                await this.GoToAsync(nameof(Pages.BoekenPagina));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"QuickNewBook_Clicked error: {ex}");
            }
        }

        private async void QuickNewMember_Clicked(object sender, EventArgs e)
        {
            try
            {
                await this.GoToAsync(nameof(Pages.LedenPagina));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"QuickNewMember_Clicked error: {ex}");
            }
        }

        // Runtime validation helper to surface missing pages / null controls early.
        private void ValidatePages()
        {
            // Validate that registered page types actually exist
            var registered = new (string Route, System.Type Type)[]
            {
                (nameof(Pages.BoekenPagina), typeof(Pages.BoekenPagina)),
                (nameof(Pages.LedenPagina), typeof(Pages.LedenPagina)),
                (nameof(Pages.UitleningenPagina), typeof(Pages.UitleningenPagina)),
                (nameof(Pages.CategorieenPagina), typeof(Pages.CategorieenPagina)),
                (nameof(Pages.Account.ProfilePage), typeof(Pages.Account.ProfilePage)),
                (nameof(Pages.MainPage), typeof(Pages.MainPage)),
                (nameof(Pages.Account.LoginPage), typeof(Pages.Account.LoginPage)),
                (nameof(Pages.InstellingenPagina), typeof(Pages.InstellingenPagina)),
                (nameof(Pages.BoekDetailsPage), typeof(Pages.BoekDetailsPage)),
                (nameof(Pages.LidDetailsPage), typeof(Pages.LidDetailsPage))
            };

            foreach (var r in registered)
            {
                if (r.Type == null)
                {
                    Debug.WriteLine($"Route registration: type for route '{r.Route}' is null.");
                }
            }

            // Validate important controls were created by XAML
            var controls = new (string Name, object? Control)[]
            {
                ("LanguagePicker", LanguagePicker),
                ("QuickNewBookButton", QuickNewBookButton),
                ("QuickNewMemberButton", QuickNewMemberButton),
                ("ProfileMenuItem", ProfileMenuItem),
                ("LogoutMenuItem", LogoutMenuItem),
                ("ThemeToggleButton", ThemeToggleButton)
            };

            foreach (var c in controls)
            {
                if (c.Control is null)
                {
                    Debug.WriteLine($"Missing control: {c.Name} is null. Check AppShell.xaml for correct x:Name.");
                }
            }

            // Basic sanity for commands
            if (LoginCommand == null) Debug.WriteLine("LoginCommand is null.");
            if (OpenSettingsCommand == null) Debug.WriteLine("OpenSettingsCommand is null.");
        }
    }
}
