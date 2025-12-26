using System;
using System.Linq;
using Biblio_Models.Entiteiten;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel
    {
        // When a loan is selected, populate the form with its details
        partial void OnSelectedUitleningChanged(Lenen? value)
        {
            // MUST run on Main Thread for Android
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (value == null)
                    {
                        // Clear form
                        SelectedBoek = null;
                        SelectedLid = null;
                        StartDate = DateTime.Now;
                        DueDate = DateTime.Now.AddDays(14);
                        ReturnedAt = null;
                        return;
                    }

                    // Populate form with selected loan data
                    StartDate = value.StartDate;
                    DueDate = value.DueDate;
                    ReturnedAt = value.ReturnedAt;

                    // Find and set the matching Boek in BoekenList
                    if (value.Boek != null && BoekenList != null)
                    {
                        var matchingBoek = BoekenList.FirstOrDefault(b => b.Id == value.BoekId);
                        SelectedBoek = matchingBoek ?? value.Boek;
                    }
                    else
                    {
                        SelectedBoek = value.Boek;
                    }

                    // Find and set the matching Lid in LedenList
                    if (value.Lid != null && LedenList != null)
                    {
                        var matchingLid = LedenList.FirstOrDefault(l => l.Id == value.LidId);
                        SelectedLid = matchingLid ?? value.Lid;
                    }
                    else
                    {
                        SelectedLid = value.Lid;
                    }

                    // Update return status
                    if (value.ReturnedAt.HasValue)
                    {
                        SelectedReturnStatus = Localize("ReturnedOption");
                    }
                    else
                    {
                        SelectedReturnStatus = Localize("Return");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OnSelectedUitleningChanged error: {ex}");
                }
            });
        }

        // When a member is selected in the Lid picker, try to select a related loan so the form (including the return-status picker) updates.
        partial void OnSelectedLidChanged(Lid? value)
        {
            // MUST run on Main Thread for Android
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
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
            });
        }
    }
}
