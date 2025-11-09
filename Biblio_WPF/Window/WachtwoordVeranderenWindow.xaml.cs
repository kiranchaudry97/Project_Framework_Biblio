using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.ComponentModel;

namespace Biblio_WPF.Window
{
    /// <summary>
    /// Interaction logic for WachtwoordVeranderenWindw.xaml
    /// zie commit bericht 
    /// </summary>
    public partial class WachtwoordVeranderenWindw : Page
    {
        private Biblio_WPF.ViewModels.SecurityViewModel? _security;

        public WachtwoordVeranderenWindw()
        {
            InitializeComponent();
            this.Loaded += WachtwoordVeranderenWindw_Loaded;
            this.Unloaded += WachtwoordVeranderenWindw_Unloaded;
        }

        private void WachtwoordVeranderenWindw_Loaded(object? sender, RoutedEventArgs e)
        {
            var saveBtn = this.FindName("SavePwdBtn") as Button;
            var cancelBtn = this.FindName("CancelBtn") as Button;
            if (saveBtn != null) saveBtn.Click += OnSavePassword;
            if (cancelBtn != null) cancelBtn.Click += OnCancel;

            // wire show toggles using named handlers
            var curBtn = this.FindName("CurrentShowBtn") as ToggleButton;
            var newBtn = this.FindName("NewShowBtn") as ToggleButton;
            var confBtn = this.FindName("ConfirmShowBtn") as ToggleButton;
            if (curBtn != null) { curBtn.Checked += CurrentShow_Checked; curBtn.Unchecked += CurrentShow_Unchecked; curBtn.IsChecked = false; SetToggleLabel(curBtn, false); SetToolTip(curBtn, false); }
            if (newBtn != null) { newBtn.Checked += NewShow_Checked; newBtn.Unchecked += NewShow_Unchecked; newBtn.IsChecked = false; SetToggleLabel(newBtn, false); SetToolTip(newBtn, false); }
            if (confBtn != null) { confBtn.Checked += ConfirmShow_Checked; confBtn.Unchecked += ConfirmShow_Unchecked; confBtn.IsChecked = false; SetToggleLabel(confBtn, false); SetToolTip(confBtn, false); }

            var svc = Biblio_WPF.App.AppHost?.Services;
            _security = svc?.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
            if (_security != null)
            {
                _security.PropertyChanged += Security_PropertyChanged;
                PopulateProfileDisplay();
            }
        }

        private void WachtwoordVeranderenWindw_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (_security != null)
            {
                _security.PropertyChanged -= Security_PropertyChanged;
                _security = null;
            }

            // unhook toggle handlers
            var curBtn = this.FindName("CurrentShowBtn") as ToggleButton;
            var newBtn = this.FindName("NewShowBtn") as ToggleButton;
            var confBtn = this.FindName("ConfirmShowBtn") as ToggleButton;
            if (curBtn != null) { curBtn.Checked -= CurrentShow_Checked; curBtn.Unchecked -= CurrentShow_Unchecked; }
            if (newBtn != null) { newBtn.Checked -= NewShow_Checked; newBtn.Unchecked -= NewShow_Unchecked; }
            if (confBtn != null) { confBtn.Checked -= ConfirmShow_Checked; confBtn.Unchecked -= ConfirmShow_Unchecked; }
        }

        private void CurrentShow_Checked(object? sender, RoutedEventArgs e) => ShowPassword("CurrentPwdBox","CurrentPwdText", sender as ToggleButton, true);
        private void CurrentShow_Unchecked(object? sender, RoutedEventArgs e) => ShowPassword("CurrentPwdBox","CurrentPwdText", sender as ToggleButton, false);
        private void NewShow_Checked(object? sender, RoutedEventArgs e) => ShowPassword("NewPwdBox","NewPwdText", sender as ToggleButton, true);
        private void NewShow_Unchecked(object? sender, RoutedEventArgs e) => ShowPassword("NewPwdBox","NewPwdText", sender as ToggleButton, false);
        private void ConfirmShow_Checked(object? sender, RoutedEventArgs e) => ShowPassword("ConfirmPwdBox","ConfirmPwdText", sender as ToggleButton, true);
        private void ConfirmShow_Unchecked(object? sender, RoutedEventArgs e) => ShowPassword("ConfirmPwdBox","ConfirmPwdText", sender as ToggleButton, false);

        private void SetToggleLabel(ToggleButton btn, bool showing)
        {
            if (btn == null) return;
            var label = btn.Content as TextBlock;
            if (label == null) return;
            label.Text = showing ? "Verberg" : "Toon";
        }
        private void SetToolTip(ToggleButton btn, bool show)
        {
            if (btn == null) return;
            btn.ToolTip = show ? "Verberg wachtwoord" : "Toon wachtwoord";
        }

        private void ShowPassword(string pwdBoxName, string textBoxName, ToggleButton btn, bool show)
        {
            var pwdBox = this.FindName(pwdBoxName) as PasswordBox;
            var txtBox = this.FindName(textBoxName) as TextBox;
            if (pwdBox == null || txtBox == null || btn == null) return;

            if (show)
            {
                txtBox.Text = pwdBox.Password;
                txtBox.Visibility = Visibility.Visible;
                pwdBox.Visibility = Visibility.Collapsed;
                // update label and tooltip
                SetToggleLabel(btn, true);
                SetToolTip(btn, true);
            }
            else
            {
                pwdBox.Password = txtBox.Text;
                txtBox.Visibility = Visibility.Collapsed;
                pwdBox.Visibility = Visibility.Visible;
                SetToggleLabel(btn, false);
                SetToolTip(btn, false);
            }
        }

        private void Security_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.CurrentEmail)
                || e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.FullName)
                || e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.IsAdmin)
                || e.PropertyName == nameof(Biblio_WPF.ViewModels.SecurityViewModel.IsStaff))
            {
                Dispatcher.Invoke(() => PopulateProfileDisplay());
            }
        }

        private void PopulateProfileDisplay()
        {
            if (_security == null) return;
            var emailBox = this.FindName("EmailDisplayBox") as TextBox;
            var fullNameBox = this.FindName("FullNameDisplayBox") as TextBox;
            var roleBox = this.FindName("RoleDisplayBox") as TextBox;

            if (emailBox != null) emailBox.Text = _security.CurrentEmail ?? string.Empty;
            if (fullNameBox != null) fullNameBox.Text = _security.FullName ?? string.Empty;
            if (roleBox != null)
            {
                if (_security.IsAdmin) roleBox.Text = "Beheerder";
                else if (_security.IsStaff) roleBox.Text = "Medewerker";
                else roleBox.Text = "Gebruiker";
            }
        }

        private void ClearErrors()
        {
            var cErr = this.FindName("CurrentPwdError") as TextBlock;
            var nErr = this.FindName("NewPwdError") as TextBlock;
            var fErr = this.FindName("ConfirmPwdError") as TextBlock;
            var status = this.FindName("StatusText") as TextBlock;
            if (cErr != null) cErr.Text = string.Empty;
            if (nErr != null) nErr.Text = string.Empty;
            if (fErr != null) fErr.Text = string.Empty;
            if (status != null) { status.Text = string.Empty; status.Foreground = Brushes.Gray; }
        }

        private string ReadPassword(string pwdBoxName, string textBoxName)
        {
            var pwdBox = this.FindName(pwdBoxName) as PasswordBox;
            var txtBox = this.FindName(textBoxName) as TextBox;
            if (txtBox != null && txtBox.Visibility == Visibility.Visible)
                return txtBox.Text ?? string.Empty;
            return pwdBox?.Password ?? string.Empty;
        }

        private async void OnSavePassword(object? sender, RoutedEventArgs e)
        {
            ClearErrors();

            var current = ReadPassword("CurrentPwdBox", "CurrentPwdText");
            var nw = ReadPassword("NewPwdBox", "NewPwdText");
            var conf = ReadPassword("ConfirmPwdBox", "ConfirmPwdText");

            var cErr = this.FindName("CurrentPwdError") as TextBlock;
            var nErr = this.FindName("NewPwdError") as TextBlock;
            var fErr = this.FindName("ConfirmPwdError") as TextBlock;
            var status = this.FindName("StatusText") as TextBlock;

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(current))
            {
                if (cErr != null) cErr.Text = "Huidig wachtwoord is verplicht.";
                hasError = true;
            }

            // Password rules
            if (string.IsNullOrWhiteSpace(nw))
            {
                if (nErr != null) nErr.Text = "Nieuw wachtwoord is verplicht.";
                hasError = true;
            }
            else
            {
                // min length
                if (nw.Length < 8)
                {
                    if (nErr != null) nErr.Text = "Minimaal 8 tekens.";
                    hasError = true;
                }
                else
                {
                    // complexity: upper, lower, digit
                    bool hasUpper = nw.Any(char.IsUpper);
                    bool hasLower = nw.Any(char.IsLower);
                    bool hasDigit = nw.Any(char.IsDigit);
                    if (!hasUpper || !hasLower || !hasDigit)
                    {
                        if (nErr != null) nErr.Text = "Gebruik hoofdletter, kleine letter en cijfer.";
                        hasError = true;
                    }
                }

                if (nw == current)
                {
                    if (nErr != null) nErr.Text = "Nieuw wachtwoord moet verschillen van huidig wachtwoord.";
                    hasError = true;
                }
            }

            if (nw != conf)
            {
                if (fErr != null) fErr.Text = "Bevestiging komt niet overeen.";
                hasError = true;
            }

            if (hasError)
            {
                if (status != null) { status.Text = "Er zijn validatiefouten."; status.Foreground = Brushes.Red; }
                return;
            }

            var services = Biblio_WPF.App.AppHost?.Services;
            if (services == null)
            {
                if (status != null) { status.Text = "Applicatie services niet beschikbaar."; status.Foreground = Brushes.Red; }
                return;
            }

            var security = _security ?? services.GetService<Biblio_WPF.ViewModels.SecurityViewModel>();
            if (security == null || string.IsNullOrWhiteSpace(security.CurrentEmail))
            {
                if (status != null) { status.Text = "Geen ingelogde gebruiker gevonden."; status.Foreground = Brushes.Red; }
                return;
            }

            var userMgr = services.GetService<UserManager<Biblio_Models.Entiteiten.AppUser>>();
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

            // Verify current password
            var valid = await userMgr.CheckPasswordAsync(user, current);
            if (!valid)
            {
                if (cErr != null) cErr.Text = "Huidig wachtwoord is onjuist.";
                if (status != null) { status.Text = "Huidig wachtwoord is onjuist."; status.Foreground = Brushes.Red; }
                return;
            }

            var result = await userMgr.ChangePasswordAsync(user, current, nw);
            if (result.Succeeded)
            {
                if (status != null) { status.Text = "Wachtwoord succesvol gewijzigd."; status.Foreground = Brushes.Green; }
                // small delay to let user see status then close
                await Task.Delay(700);
                var wnd = System.Windows.Window.GetWindow(this);
                wnd?.Close();
            }
            else
            {
                var msgs = string.Join(';', result.Errors.Select(err => err.Description));
                if (status != null) { status.Text = $"Fout bij wijzigen wachtwoord: {msgs}"; status.Foreground = Brushes.Red; }
            }
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            var wnd = System.Windows.Window.GetWindow(this);
            wnd?.Close();
        }
    }
}
