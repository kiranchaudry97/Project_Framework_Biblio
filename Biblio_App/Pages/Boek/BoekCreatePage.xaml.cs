using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using System.Linq;

namespace Biblio_App.Pages.Boek
{
    public partial class BoekCreatePage : ContentPage
    {
        public BoekCreatePage(BoekenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        public void ApplyQueryAttributes(System.Collections.Generic.IDictionary<string, object> query)
        {
            if (query == null) return;
            if (query.TryGetValue("boekId", out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out var id))
                {
                    // stel het geselecteerde boek in zodat de viewmodel het in bewerkbare velden laadt
                    var vm = BindingContext as BoekenViewModel;
                    if (vm != null)
                    {
                        // zoek het boek eerst in de reeds geladen collectie
                        var b = vm.Boeken.FirstOrDefault(x => x.Id == id);
                        if (b != null) vm.SelectedBoek = b;
                        else
                        {
                            // anders asynchroon uit de database laden (fire-and-forget)
                            _ = vm.EnsureCategoriesLoadedAsync();
                            // na EnsureCategoriesLoadedAsync opnieuw proberen te vinden
                            var found = vm.Boeken.FirstOrDefault(x => x.Id == id);
                            if (found != null) vm.SelectedBoek = found;
                        }
                    }
                }
            }
        }

        private async void OnCancelClicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
