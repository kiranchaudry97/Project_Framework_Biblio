using Microsoft.Maui.Controls;
using Biblio_Models.Entiteiten;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

namespace Biblio_App.Pages
{
    public partial class BoekDetailsPage : ContentPage, IQueryAttributable
    {
        public BoekDetailsPage()
        {
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query == null) return;
            if (query.TryGetValue("boekId", out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out var id))
                {
                    _ = LoadBoekAsync(id);
                }
            }
        }

        private async Task LoadBoekAsync(int id)
        {
            try
            {
                var svc = App.Current?.Handler?.MauiContext?.Services;
                var factory = svc?.GetService<IDbContextFactory<BiblioDbContext>>();
                if (factory != null)
                {
                    using var db = factory.CreateDbContext();
                    var boek = await db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
                    if (boek != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => BindingContext = boek);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("..", true);
            }
            catch { }
        }
    }
}
