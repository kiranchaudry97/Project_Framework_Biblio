using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel
    {
        // helper: attempt to show a toast using CommunityToolkit.Maui if available, otherwise fallback to DisplayAlert
        private async Task ShowToastOrAlertAsync(string message)
        {
            try
            {
                // try to find the CommunityToolkit.Maui Toast type by name
                var toastType = Type.GetType("CommunityToolkit.Maui.Alerts.Toast, CommunityToolkit.Maui");
                if (toastType != null)
                {
                    // prefer static Make(string) method
                    var make = toastType.GetMethod("Make", new[] { typeof(string) });
                    object? toastInstance = null;
                    if (make != null)
                    {
                        toastInstance = make.Invoke(null, new object[] { message });
                    }

                    if (toastInstance != null)
                    {
                        var show = toastInstance.GetType().GetMethod("Show", Type.EmptyTypes);
                        if (show != null)
                        {
                            var ret = show.Invoke(toastInstance, null);
                            if (ret is Task t) await t;
                            return;
                        }
                    }
                }
            }
            catch { }

            // fallback to DisplayAlert
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(Localize("Ready"), message, Localize("OK"));
                    }
                });
            }
            catch { }
        }

        // Public helper to persist the current SelectedUitlening ReturnedAt value and show a toast
        public async Task<bool> SaveSelectedReturnStatusAsync()
        {
            try
            {
                if (SelectedUitlening == null) return false;

                try
                {
                    await PersistReturnedStatusAsync(SelectedUitlening, ReturnedAt);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    try { await ShowToastOrAlertAsync(Localize("ErrorUpdatingLoan")); } catch { }
                    return false;
                }

                // After successful persist, ensure all visible instances of the same loan are updated
                try
                {
                    // compute flags
                    var isLate = string.Equals(SelectedReturnStatus, Localize("Late"), StringComparison.OrdinalIgnoreCase);
                    var isReturn = string.Equals(SelectedReturnStatus, Localize("Return"), StringComparison.OrdinalIgnoreCase);

                    // update any items in the collection that match the persisted Id
                    if (SelectedUitlening != null)
                    {
                        var id = SelectedUitlening.Id;
                        
                        // MUST run on Main Thread for Android - UI collections update
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                for (int i = 0; i < Uitleningen.Count; i++)
                                {
                                    try
                                    {
                                        var it = Uitleningen[i];
                                        if (it != null && it.Id == id)
                                        {
                                            // create a new instance copy so CollectionView sees replacement
                                            var copy = new Biblio_Models.Entiteiten.Lenen
                                            {
                                                Id = SelectedUitlening.Id,
                                                BoekId = SelectedUitlening.BoekId,
                                                Boek = SelectedUitlening.Boek,
                                                LidId = SelectedUitlening.LidId,
                                                Lid = SelectedUitlening.Lid,
                                                StartDate = SelectedUitlening.StartDate,
                                                DueDate = SelectedUitlening.DueDate,
                                                ReturnedAt = SelectedUitlening.ReturnedAt,
                                                ForceLate = isLate && !SelectedUitlening.ReturnedAt.HasValue,
                                                ForceNotLate = isReturn && !SelectedUitlening.ReturnedAt.HasValue,
                                                IsClosed = SelectedUitlening.IsClosed
                                            };

                                            Uitleningen[i] = copy;
                                        }
                                    }
                                    catch { }
                                }

                                // also ensure SelectedUitlening points to the collection's instance (if any)
                                var firstIdx = Uitleningen.ToList().FindIndex(u => u.Id == SelectedUitlening.Id);
                                if (firstIdx >= 0)
                                {
                                    SelectedUitlening = Uitleningen[firstIdx];
                                }
                            }
                            catch { }
                        });
                    }
                }
                catch { }

                // success
                var message = ReturnedAt.HasValue ? Localize("BookMarkedReturned") : Localize("SavedLoan");
                try
                {
                    await ShowToastOrAlertAsync(message);
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                try { await ShowToastOrAlertAsync(Localize("ErrorUpdatingLoan")); } catch { }
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }
        }
    }
}
