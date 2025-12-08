using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System.Linq;

namespace Biblio_App.Pages
{
    public partial class BoekenPagina : ContentPage
    {
        private BoekenViewModel VM => BindingContext as BoekenViewModel;

        public BoekenPagina(BoekenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            if (vm != null)
                vm.PropertyChanged += Vm_PropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Ensure categories and books are loaded when the page appears so the category picker shows all items
            if (VM != null)
            {
                await VM.EnsureCategoriesLoadedAsync();

                // if no selected filter set, pick first (typically 'Alle')
                if (VM.SelectedFilterCategorie == null && VM.Categorien?.Count > 0)
                {
                    VM.SelectedFilterCategorie = VM.Categorien.FirstOrDefault();
                }
            }
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

        // New click handlers for the image buttons
        private async void OnDetailsClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Boek boek)
                {
                    // navigate to a details page or show alert with basic info
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
                // navigate to the create page route using fully-qualified type name for nameof
                await Shell.Current.GoToAsync(nameof(Biblio_App.Pages.Boek.BoekCreatePage));
            }
            catch { }
        }
    }
}
