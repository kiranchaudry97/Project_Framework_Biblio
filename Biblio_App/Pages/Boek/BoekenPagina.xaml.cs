using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System.Linq;
using System;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using Biblio_App.Services;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;

namespace Biblio_App.Pages
{
    public partial class BoekenPagina : ContentPage, ILocalizable
    {
        private BoekenViewModel VM => BindingContext as BoekenViewModel;
        private ILanguageService? _languageService;

        public BoekenPagina(BoekenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            if (vm != null)
                vm.PropertyChanged += Vm_PropertyChanged;

            // resolve language service if possible
            try
            {
                _languageService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
            }

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Zorg dat categorieën en boeken geladen zijn wanneer de pagina verschijnt zodat de categorie-picker alle items toont
            if (VM != null)
            {
                await VM.EnsureCategoriesLoadedAsync();

                // als er geen geselecteerde filter is, kies de eerste (meestal 'Alle')
                if (VM.SelectedFilterCategorie == null && VM.Categorien?.Count > 0)
                {
                    VM.SelectedFilterCategorie = VM.Categorien.FirstOrDefault();
                }
            }

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

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BoekenViewModel.HasValidationErrors) || e.PropertyName == nameof(BoekenViewModel.ValidationMessage))
            {
                FocusFirstError();
            }

            if (e.PropertyName == nameof(BoekenViewModel.SelectedBoek))
            {
                if (VM?.SelectedBoek == null)
                {
                    TitelEntry?.Focus();
                }
            }
        }

        private void FocusFirstError()
        {
            if (VM == null) return;
            if (!string.IsNullOrEmpty(VM.TitelError))
            {
                TitelEntry?.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.AuteurError))
            {
                AuteurEntry?.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.IsbnError))
            {
                IsbnEntry?.Focus();
                return;
            }
        }

        // Nieuwe click-handlers voor de afbeeldingsknoppen
        private async void OnDetailsClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                {
                    // navigeer naar de detailpagina of toon een melding met basisinformatie
                    await DisplayAlert("Details", $"{boek.Titel}\n{boek.Auteur}\nISBN: {boek.Isbn}", "OK");
                }
            }
            catch { }
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                {
                    VM.SelectedBoek = boek;
                }
            }
            catch { }
        }

        private async void OnCreateNewBookClicked(object sender, EventArgs e)
        {
            try
            {
                // navigeer naar de aanmaakpagina via de geregistreerde route (typename gebruikt voor nameof)
                await Shell.Current.GoToAsync(nameof(Biblio_App.Pages.Boek.BoekCreatePage));
            }
            catch { }
        }

        // Toon volledige tekst wanneer op een afgeknot label wordt getapt
        private async void OnLabelTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Label lbl)
                {
                    var text = lbl.Text;
                    // Probeer meer context uit BindingContext te halen indien beschikbaar (boek)
                    if (lbl.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                    {
                        // bepaal welke eigenschap is aangeraakt op basis van de kolom (gebruik index van parent grid children)
                        // fallback: toon titel + auteur + isbn
                        text = $"{boek.Titel}\n{boek.Auteur}\nISBN: {boek.Isbn}\nCategorie: {boek.CategorieID}";
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        await DisplayAlert("Volledige tekst", text, "OK");
                    }
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
                    try
                    {
                        try
                        {
                            if (VM is Biblio_App.Services.ILocalizable loc)
                            {
                                loc.UpdateLocalizedStrings();
                            }
                            else
                            {
                                VM?.RefreshLocalizedStrings();
                            }
                        }
                        catch { }

                        try { this.ForceLayout(); this.InvalidateMeasure(); }
                        catch { }
                    }
                    catch { }
                });
            }
            catch { }
        }

        public void UpdateLocalizedStrings()
        {
#if DEBUG
            try
            {
                System.Diagnostics.Debug.WriteLine("BoekenPagina.UpdateLocalizedStrings called");
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }
            catch { }
#endif
            try
            {
                if (VM is Biblio_App.Services.ILocalizable locVm)
                {
                    try { locVm.UpdateLocalizedStrings(); } catch { }
                }
                else
                {
                    try { VM?.RefreshLocalizedStrings(); } catch { }
                }

                try
                {
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { this.ForceLayout(); this.InvalidateMeasure(); }
                        catch { }
                    });
                }
                catch { }
            }
            catch { }
        }
    }
}
