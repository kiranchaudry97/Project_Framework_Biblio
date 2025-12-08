using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System;

namespace Biblio_App.Pages
{
    public partial class InstellingenPagina : ContentPage
    {
        private InstellingenViewModel VM => BindingContext as InstellingenViewModel;

        public InstellingenPagina(InstellingenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (VM != null)
            {
                await VM.LoadDatabaseInfoAsync();
            }
        }

        private async void OnResetDbClicked(object sender, EventArgs e)
        {
            var ok = await DisplayAlert("Bevestig", "Verwijder lokale database en her-seed?", "Ja", "Nee");
            if (!ok) return;

            if (VM != null)
            {
                var res = await VM.ResetAndSeedLocalDatabaseAsync();
                await DisplayAlert("Resultaat", res ? "Database verwijderd en opnieuw seeded." : "Kon database niet resetten.", "OK");
            }
        }
    }
}
