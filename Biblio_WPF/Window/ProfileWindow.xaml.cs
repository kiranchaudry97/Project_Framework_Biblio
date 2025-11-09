using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for ProfileWindow.xaml
    /// zie commit bericht
    /// </summary>
    public partial class ProfileWindow : Page
    {
        private Biblio_WPF.ViewModels.SecurityViewModel? _security;

        public ProfileWindow()
        {
            InitializeComponent();
            this.Loaded += ProfileWindow_Loaded;
            this.Unloaded += ProfileWindow_Unloaded;
        }

        private void ProfileWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            _security = svc?.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
            if (_security != null)
            {
                _security.PropertyChanged += Security_PropertyChanged;
                PopulateFromSecurity();
            }
        }

        private void ProfileWindow_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (_security != null)
            {
                _security.PropertyChanged -= Security_PropertyChanged;
                _security = null;
            }
        }

        private void Security_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // update UI when relevant security properties change
            if (e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.CurrentEmail)
                || e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.FullName)
                || e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.IsAdmin)
                || e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.IsStaff))
            {
                // ensure UI update on dispatcher thread
                Dispatcher.Invoke(() => PopulateFromSecurity());
            }
        }

        private void PopulateFromSecurity()
        {
            if (_security == null) return;

            var emailBox = this.FindName("EmailBox") as TextBox;
            var fullNameBox = this.FindName("FullNameBox") as TextBox;
            var roleBox = this.FindName("RoleBox") as TextBox;
            var status = this.FindName("StatusText") as TextBlock;

            if (emailBox != null) emailBox.Text = _security.CurrentEmail ?? string.Empty;
            if (fullNameBox != null) fullNameBox.Text = _security.FullName ?? string.Empty;

            if (roleBox != null)
            {
                if (_security.IsAdmin) roleBox.Text = "Beheerder";
                else if (_security.IsStaff) roleBox.Text = "Medewerker";
                else roleBox.Text = "Gebruiker";
            }

            if (status != null)
            {
                status.Text = string.Empty;
                status.Foreground = Brushes.Gray;
            }
        }

        private void ClearErrors()
        {
            var fErr = this.FindName("FullNameError") as TextBlock;
            var status = this.FindName("StatusText") as TextBlock;
            if (fErr != null) fErr.Text = string.Empty;
            if (status != null) { status.Text = string.Empty; status.Foreground = Brushes.Gray; }
        }

        private async void OnSaveProfile(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            var svc = Biblio_WPF.App.AppHost?.Services;
            var security = _security ?? svc?.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
            var status = this.FindName("StatusText") as TextBlock;
            var fErr = this.FindName("FullNameError") as TextBlock;
            var fullNameBox = this.FindName("FullNameBox") as TextBox;

            if (security == null || string.IsNullOrWhiteSpace(security.CurrentEmail))
            {
                if (status != null) { status.Text = "Geen ingelogde gebruiker."; status.Foreground = Brushes.Red; }
                return;
            }

            var fullName = fullNameBox?.Text?.Trim() ?? string.Empty;
            bool hasError = false;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                if (fErr != null) fErr.Text = "Volledige naam is verplicht.";
                hasError = true;
            }
            else if (fullName.Length < 3)
            {
                if (fErr != null) fErr.Text = "Minimaal 3 tekens.";
                hasError = true;
            }

            if (hasError)
            {
                if (status != null) { status.Text = "Er zijn validatiefouten."; status.Foreground = Brushes.Red; }
                return;
            }

            var userMgr = svc.GetService<UserManager<Biblio_Models.Entiteiten.AppUser>>();
            if (userMgr == null)
            {
                if (status != null) { status.Text = "UserManager niet beschikbaar."; status.Foreground = Brushes.Red; }
                return;
            }

            var user = await userMgr.FindByEmailAsync(security.CurrentEmail);
            if (user == null)
            {
                if (status != null) { status.Text = "Gebruiker niet gevonden."; status.Foreground = Brushes.Red; }
                return;
            }

            user.FullName = fullName;
            var result = await userMgr.UpdateAsync(user);
            if (result.Succeeded)
            {
                // update security viewmodel full name as well
                security.SetUser(security.CurrentEmail, security.IsAdmin, security.IsStaff, fullName);
                if (status != null) { status.Text = "Profiel succesvol opgeslagen."; status.Foreground = Brushes.Green; }
            }
            else
            {
                var msgs = string.Join(';', result.Errors.Select(err => err.Description));
                if (status != null) { status.Text = $"Fout bij opslaan: {msgs}"; status.Foreground = Brushes.Red; }
            }
        }

        private void OnChangePassword(object sender, RoutedEventArgs e)
        {
            var svc = Biblio_WPF.App.AppHost?.Services;
            if (svc == null) return;
            var page = svc.GetService<WachtwoordVeranderenWindw>();
            if (page != null)
            {
                var w = new System.Windows.Window { Title = "Wachtwoord wijzigen", Content = page, Owner = System.Windows.Window.GetWindow(this), Width = 600, Height = 400 };
                w.ShowDialog();
            }
        }
    }
}
