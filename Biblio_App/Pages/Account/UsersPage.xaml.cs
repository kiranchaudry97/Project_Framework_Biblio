using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;
using Biblio_App.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Resources;
using System.Globalization;

namespace Biblio_App.Pages.Account
{
    public partial class UsersPage : ContentPage, ILocalizable
    {
        // ViewModel met gebruikerslijst/rollen (admin/staff)
        private UsersViewModel VM => BindingContext as UsersViewModel;

        // Language service om te kunnen hertekenen bij taalwijziging
        private ILanguageService? _languageService;

        public static readonly BindableProperty AdminLabelTextProperty = BindableProperty.Create(nameof(AdminLabelText), typeof(string), typeof(UsersPage), default(string));
        public static readonly BindableProperty StaffLabelTextProperty = BindableProperty.Create(nameof(StaffLabelText), typeof(string), typeof(UsersPage), default(string));

        public string AdminLabelText { get => (string)GetValue(AdminLabelTextProperty); set => SetValue(AdminLabelTextProperty, value); }
        public string StaffLabelText { get => (string)GetValue(StaffLabelTextProperty); set => SetValue(StaffLabelTextProperty, value); }

        public UsersPage(UsersViewModel vm)
        {
            InitializeComponent();

            // MVVM: ViewModel koppelen
            BindingContext = vm;
            try { _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }

            // Init teksten voor deze pagina (simpel gehouden)
            UpdateLocalizedStrings();
        }

        public void UpdateLocalizedStrings()
        {
            try
            {
                // Deze pagina heeft momenteel vooral vaste NL teksten.
                // We zetten ze hier zodat we later makkelijk kunnen uitbreiden naar echte resx-vertalingen.
                var title = "Gebruikers";
                try { if (TitleLabel != null) TitleLabel.Text = title; } catch { }
                try { if (HeaderLabel != null) HeaderLabel.Text = "Gebruikers beheer"; } catch { }
                try { if (NewButton != null) NewButton.Text = "Nieuw"; } catch { }
                try { if (SaveRolesButton != null) SaveRolesButton.Text = "Opslaan rollen"; } catch { }

                // set page-level texts for DataTemplate bindings
                try { AdminLabelText = "Admin"; } catch { }
                try { StaffLabelText = "Medewerker"; } catch { }
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
                    // Subscribe: als taal verandert, teksten opnieuw zetten
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
                    // Unsubscribe om memory leaks te vermijden
                    _languageService.LanguageChanged -= LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => UpdateLocalizedStrings());
            }
            catch { }
        }
    }
}
