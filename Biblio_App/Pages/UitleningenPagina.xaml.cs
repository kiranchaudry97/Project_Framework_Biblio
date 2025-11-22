using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;

namespace Biblio_App.Pages
{
    public partial class UitleningenPagina : ContentPage
    {
        private UitleningenViewModel VM => BindingContext as UitleningenViewModel;

        public UitleningenPagina(UitleningenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            // subscription moved to OnAppearing
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (VM != null)
            {
                VM.PropertyChanged -= Vm_PropertyChanged;
                VM.PropertyChanged += Vm_PropertyChanged;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (VM != null)
            {
                VM.PropertyChanged -= Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UitleningenViewModel.HasValidationErrors) || e.PropertyName == nameof(UitleningenViewModel.ValidationMessage))
            {
                FocusFirstError();
            }

            if (e.PropertyName == nameof(UitleningenViewModel.SelectedUitlening))
            {
                if (VM?.SelectedUitlening == null)
                {
                    BoekPicker.Focus();
                }
            }
        }

        private void FocusFirstError()
        {
            if (VM == null) return;
            if (!string.IsNullOrEmpty(VM.BoekError))
            {
                BoekPicker.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.LidError))
            {
                LidPicker.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.StartDateError))
            {
                StartDatePicker.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.DueDateError))
            {
                DueDatePicker.Focus();
                return;
            }
        }
    }
}