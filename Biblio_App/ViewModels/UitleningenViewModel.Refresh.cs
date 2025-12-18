using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Biblio_Models.Entiteiten;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel
    {
        // Force a UI refresh for the currently selected loan by replacing it with a shallow copy in the collection.
        public void RefreshSelectedLoanUI()
        {
            try
            {
                if (SelectedUitlening == null) return;

                var id = SelectedUitlening.Id;
                var idx = Uitleningen.ToList().FindIndex(u => u.Id == id);
                var item = SelectedUitlening;
                var copy = new Lenen
                {
                    Id = item.Id,
                    BoekId = item.BoekId,
                    Boek = item.Boek,
                    LidId = item.LidId,
                    Lid = item.Lid,
                    StartDate = item.StartDate,
                    DueDate = item.DueDate,
                    ReturnedAt = item.ReturnedAt,
                    ForceLate = item.ForceLate,
                    ForceNotLate = item.ForceNotLate,
                    IsClosed = item.IsClosed
                };

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (idx >= 0)
                        {
                            Uitleningen[idx] = copy;
                        }
                        else
                        {
                            if (!Uitleningen.Any(u => u.Id == id)) Uitleningen.Insert(0, copy);
                        }

                        SelectedUitlening = copy;
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}
