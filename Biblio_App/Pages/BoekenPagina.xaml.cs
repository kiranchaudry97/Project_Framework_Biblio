using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

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
    }
}