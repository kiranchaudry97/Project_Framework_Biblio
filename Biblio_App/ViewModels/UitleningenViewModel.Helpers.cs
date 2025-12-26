using System;
using System.Linq;

namespace Biblio_App.ViewModels
{
    public partial class UitleningenViewModel
    {
        // Ensure a public method exists for code-behind to refresh UI after updates
        public void RefreshSelectedLoanUI()
        {
            try
            {
                if (SelectedUitlening == null) return;

                // MUST run on Main Thread for Android
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Map ReturnedAt to SelectedReturnStatus for UI pickers
                        if (SelectedUitlening.ReturnedAt.HasValue)
                        {
                            SelectedReturnStatus = Localize("ReturnedOption");
                        }
                        else
                        {
                            SelectedReturnStatus = Localize("Return");
                        }

                        // Force notify so bindings update
                        OnPropertyChanged(nameof(SelectedReturnStatus));
                        OnPropertyChanged(nameof(SelectedUitlening));
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}