using System;
using System.Linq;
using Biblio_Models.Entiteiten;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel
    {
        // When a member is selected in the Lid picker, try to select a related loan so the form (including the return-status picker) updates.
        partial void OnSelectedLidChanged(Lid? value)
        {
            try
            {
                if (value == null)
                {
                    SelectedUitlening = null;
                    return;
                }

                // Prefer an open loan (not returned) for the selected member, otherwise pick the most recent loan.
                var match = Uitleningen.FirstOrDefault(u => u.Lid != null && u.Lid.Id == value.Id && u.ReturnedAt == null)
                            ?? Uitleningen.Where(u => u.Lid != null && u.Lid.Id == value.Id).OrderByDescending(u => u.StartDate).FirstOrDefault();

                if (match != null)
                {
                    // set SelectedUitlening which will populate the rest of the form (SelectedReturnStatus, Boek, dates...)
                    SelectedUitlening = match;
                }
                else
                {
                    // no loan found for this member: clear selected loan but keep member selected in picker
                    SelectedUitlening = null;

                    // reset form defaults
                    StartDate = DateTime.Now;
                    DueDate = DateTime.Now.AddDays(14);
                    ReturnedAt = null;

                    if (ReturnStatusOptions != null && ReturnStatusOptions.Count > 0)
                        SelectedReturnStatus = ReturnStatusOptions.First();
                }
            }
            catch { }
        }
    }
}
