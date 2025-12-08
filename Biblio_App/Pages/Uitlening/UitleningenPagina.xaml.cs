using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;

namespace Biblio_App.Pages
{
    public partial class UitleningenPagina : ContentPage
    {
        public UitleningenPagina(UitleningenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private async void OnCopyDbPathClicked(object sender, EventArgs e)
        {
            try
            {
                var lbl = this.FindByName<Label>("DbPathLabel");
                var text = lbl?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    await Clipboard.SetTextAsync(text);
                    await DisplayAlert("Gekopieerd", "Database-pad is gekopieerd naar klembord.", "OK");
                }
                else
                {
                    await DisplayAlert("Leeg", "Er is geen pad geladen.", "OK");
                }
            }
            catch { }
        }
    }
}
