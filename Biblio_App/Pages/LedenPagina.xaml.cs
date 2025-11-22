using System.Linq;
using Microsoft.Maui.Controls;
using Biblio_App.ViewModels;

namespace Biblio_App.Pages
{
    public partial class LedenPagina : ContentPage
    {
        private LedenViewModel VM => BindingContext as LedenViewModel;

        public LedenPagina(LedenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LedenViewModel.HasValidationErrors) || e.PropertyName == nameof(LedenViewModel.ValidationMessage))
            {
                FocusFirstError();
            }

            if (e.PropertyName == nameof(LedenViewModel.SelectedLid))
            {
                // If new item (selection cleared), focus first field for input
                if (VM?.SelectedLid == null)
                {
                    VoornaamEntry.Focus();
                }
            }
        }

        private void FocusFirstError()
        {
            // Focus first entry that has an error message
            if (VM == null) return;
            if (!string.IsNullOrEmpty(VM.VoornaamError))
            {
                VoornaamEntry.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.AchternaamError))
            {
                AchternaamEntry.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.EmailError))
            {
                EmailEntry.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.TelefoonError))
            {
                TelefoonEntry.Focus();
                return;
            }
            if (!string.IsNullOrEmpty(VM.AdresError))
            {
                AdresEntry.Focus();
                return;
            }
        }
    }
}