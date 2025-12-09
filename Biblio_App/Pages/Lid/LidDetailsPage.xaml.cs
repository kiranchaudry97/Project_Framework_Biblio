using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System;
using Biblio_App.ViewModels;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Pages
{
    public partial class LidDetailsPage : ContentPage, IQueryAttributable
    {
        public LidDetailsPage()
        {
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query == null) return;
            var vm = this.BindingContext as LedenViewModel;
            if (query.TryGetValue("lidId", out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out var id))
                {
                    // probeer eerst te vinden in de reeds geladen collectie
                    var l = vm?.Leden.FirstOrDefault(x => x.Id == id);
                    if (l != null)
                    {
                        vm.SelectedLid = l;
                    }
                }
            }

            // als 'edit=true' is meegegeven, laat de velden bewerkbaar (SelectedLid wordt gezet).
            // Het viewmodel zal de velden vullen wanneer SelectedLid verandert.
        }

        private async void OnBackClicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
