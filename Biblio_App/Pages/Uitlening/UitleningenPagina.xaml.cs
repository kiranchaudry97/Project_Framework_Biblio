using Biblio_App.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using Microsoft.Maui.Graphics;
using Biblio_App.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Resources;
using System.Globalization;
using Biblio_Models.Resources;
using System.Linq;

namespace Biblio_App.Pages
{
    public partial class UitleningenPagina : ContentPage, ILocalizable
    {
        private UitleningenViewModel VM => BindingContext as UitleningenViewModel;
        private ILanguageService? _language_service;
        private ResourceManager? _sharedResourceManager;

        private DateTime _lastReturnStatusChange = DateTime.MinValue;
        private readonly TimeSpan _returnStatusDebounce = TimeSpan.FromMilliseconds(300);

        public static readonly BindableProperty PageHeaderTextProperty = BindableProperty.Create(nameof(PageHeaderText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty MembersLabelProperty = BindableProperty.Create(nameof(MembersLabel), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty LoansLabelProperty = BindableProperty.Create(nameof(LoansLabel), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty BooksLabelProperty = BindableProperty.Create(nameof(BooksLabel), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty DbPathLabelTextProperty = BindableProperty.Create(nameof(DbPathLabelText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty CopyPathButtonTextProperty = BindableProperty.Create(nameof(CopyPathButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty SearchPlaceholderTextProperty = BindableProperty.Create(nameof(SearchPlaceholderText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty FilterButtonTextProperty = BindableProperty.Create(nameof(FilterButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty CategoryTitleProperty = BindableProperty.Create(nameof(CategoryTitle), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty MemberTitleProperty = BindableProperty.Create(nameof(MemberTitle), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty BookTitleProperty = BindableProperty.Create(nameof(BookTitle), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty OnlyOpenTextProperty = BindableProperty.Create(nameof(OnlyOpenText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty ViewButtonTextProperty = BindableProperty.Create(nameof(ViewButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty ReturnButtonTextProperty = BindableProperty.Create(nameof(ReturnButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty NewButtonTextProperty = BindableProperty.Create(nameof(NewButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty SaveButtonTextProperty = BindableProperty.Create(nameof(SaveButtonText), typeof(string), typeof(UitleningenPagina), default(string));
        public static readonly BindableProperty DeleteButtonTextProperty = BindableProperty.Create(nameof(DeleteButtonText), typeof(string), typeof(UitleningenPagina), default(string));

        public string PageHeaderText { get => (string)GetValue(PageHeaderTextProperty); set => SetValue(PageHeaderTextProperty, value); }
        public string MembersLabel { get => (string)GetValue(MembersLabelProperty); set => SetValue(MembersLabelProperty, value); }
        public string LoansLabel { get => (string)GetValue(LoansLabelProperty); set => SetValue(LoansLabelProperty, value); }
        public string BooksLabel { get => (string)GetValue(BooksLabelProperty); set => SetValue(BooksLabelProperty, value); }
        public string DbPathLabelText { get => (string)GetValue(DbPathLabelTextProperty); set => SetValue(DbPathLabelTextProperty, value); }
        public string CopyPathButtonText { get => (string)GetValue(CopyPathButtonTextProperty); set => SetValue(CopyPathButtonTextProperty, value); }
        public string SearchPlaceholderText { get => (string)GetValue(SearchPlaceholderTextProperty); set => SetValue(SearchPlaceholderTextProperty, value); }
        public string FilterButtonText { get => (string)GetValue(FilterButtonTextProperty); set => SetValue(FilterButtonTextProperty, value); }
        public string CategoryTitle { get => (string)GetValue(CategoryTitleProperty); set => SetValue(CategoryTitleProperty, value); }
        public string MemberTitle { get => (string)GetValue(MemberTitleProperty); set => SetValue(MemberTitleProperty, value); }
        public string BookTitle { get => (string)GetValue(BookTitleProperty); set => SetValue(BookTitleProperty, value); }
        public string OnlyOpenText { get => (string)GetValue(OnlyOpenTextProperty); set => SetValue(OnlyOpenTextProperty, value); }
        public string ViewButtonText { get => (string)GetValue(ViewButtonTextProperty); set => SetValue(ViewButtonTextProperty, value); }
        public string ReturnButtonText { get => (string)GetValue(ReturnButtonTextProperty); set => SetValue(ReturnButtonTextProperty, value); }
        public string NewButtonText { get => (string)GetValue(NewButtonTextProperty); set => SetValue(NewButtonTextProperty, value); }
        public string SaveButtonText { get => (string)GetValue(SaveButtonTextProperty); set => SetValue(SaveButtonTextProperty, value); }
        public string DeleteButtonText { get => (string)GetValue(DeleteButtonTextProperty); set => SetValue(DeleteButtonTextProperty, value); }

        public UitleningenPagina(UitleningenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            try { _language_service = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }

            InitializeSharedResourceManager();
            UpdateLocalizedStrings();
        }

        private void InitializeSharedResourceManager()
        {
            try
            {
                var webAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "Biblio_Web", StringComparison.OrdinalIgnoreCase));
                if (webAsm != null)
                {
                    foreach (var name in new[] { "Biblio_Web.Resources.Vertalingen.SharedResource", "Biblio_Web.Resources.SharedResource", "Biblio_Web.SharedResource" })
                    {
                        try
                        {
                            var rm = new ResourceManager(name, webAsm);
                            var test = rm.GetString("Members", CultureInfo.CurrentUICulture);
                            if (!string.IsNullOrEmpty(test))
                            {
                                _sharedResourceManager = rm;
                                return;
                            }
                        }
                        catch { }
                    }
                }

                var modelType = typeof(SharedModelResource);
                if (modelType != null)
                {
                    _sharedResourceManager = new ResourceManager("Biblio_Models.Resources.SharedModelResource", modelType.Assembly);
                }
            }
            catch { }
        }

        private string Localize(string key)
        {
            try
            {
                var culture = _language_service?.CurrentCulture ?? CultureInfo.CurrentUICulture;

                // prefer AppShell translation when available
                try
                {
                    var shell = AppShell.Instance;
                    if (shell != null)
                    {
                        var val = shell.Translate(key);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }
                catch { }

                if (_sharedResourceManager != null)
                {
                    try
                    {
                        var val = _sharedResourceManager.GetString(key, culture);
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    catch { }
                }

                // simple fallback for common keys
                var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "en")
                {
                    return key switch
                    {
                        "Members" => "Members",
                        "Loans" => "Loans",
                        "Books" => "Books",
                        "DbPath" => "DB path:",
                        "CopyPath" => "Copy path",
                        "SearchPlaceholder" => "Search...",
                        "Filter" => "Filter",
                        "Category" => "Category",
                        "Member" => "Member",
                        "Book" => "Book",
                        "OnlyOpen" => "Only open",
                        "View" => "View",
                        "Return" => "Return",
                        "ReturnedLabel" => "Return status",
                        "ReturnedOption" => "Returned",
                        "Late" => "Late",
                        "New" => "New",
                        "Save" => "Save",
                        "Delete" => "Delete",
                        "Copied" => "Copied",
                        "NoPath" => "No path loaded",
                        "OK" => "OK",
                        _ => key
                    };
                }

                return key switch
                {
                    "Members" => "Leden",
                    "Loans" => "Uitleningen",
                    "Books" => "Boeken",
                    "DbPath" => "DB pad:",
                    "CopyPath" => "Kopieer pad",
                    "SearchPlaceholder" => "Zoeken...",
                    "Filter" => "Filter",
                    "Category" => "Categorie",
                    "Member" => "Lid",
                    "Book" => "Boek",
                    "OnlyOpen" => "Alleen open",
                    "View" => "Inzien",
                    "Return" => "Inleveren",
                    "ReturnedLabel" => "Leverstatus",
                    "ReturnedOption" => "Geleverd",
                    "Late" => "Te laat",
                    "New" => "Nieuw",
                    "Save" => "Opslaan",
                    "Delete" => "Verwijder",
                    "Copied" => "Gekopieerd",
                    "NoPath" => "Er is geen pad geladen.",
                    "OK" => "OK",
                    _ => key
                };
            }
            catch { return key; }
        }

        public void UpdateLocalizedStrings()
        {
            PageHeaderText = Localize("Loans");
            MembersLabel = Localize("Members");
            LoansLabel = Localize("Loans");
            BooksLabel = Localize("Books");
            DbPathLabelText = Localize("DbPath");
            CopyPathButtonText = Localize("CopyPath");
            SearchPlaceholderText = Localize("SearchPlaceholder");
            FilterButtonText = Localize("Filter");
            CategoryTitle = Localize("Category");
            MemberTitle = Localize("Member");
            BookTitle = Localize("Book");
            OnlyOpenText = Localize("OnlyOpen");
            ViewButtonText = Localize("View");
            ReturnButtonText = Localize("Return");
            NewButtonText = Localize("New");
            SaveButtonText = Localize("Save");
            DeleteButtonText = Localize("Delete");

            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try { this.ForceLayout(); this.InvalidateMeasure(); }
                    catch { }
                });
            }
            catch { }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (_language_service != null)
                {
                    _language_service.LanguageChanged += LanguageService_LanguageChanged;
                }
            }
            catch { }

            // Initialize VM data without blocking UI thread
            try
            {
                if (BindingContext is UitleningenViewModel vm)
                {
                    _ = vm.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (_language_service != null)
                {
                    _language_service.LanguageChanged -= LanguageService_LanguageChanged;
                }
            }
            catch { }
        }

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Update localized strings when language changes centrally.
                    UpdateLocalizedStrings();
                });
            }
            catch { }
        }

        // Added missing Clicked handler required by XAML
        private async void OnCopyDbPathClicked(object sender, EventArgs e)
        {
            try
            {
                var lbl = this.FindByName<Label>("DbPathLabel");
                var text = lbl?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    await Clipboard.SetTextAsync(text);
                    try { await DisplayAlert(Localize("Copied"), Localize("DbPath"), Localize("OK")); } catch { }
                }
                else
                {
                    try { await DisplayAlert(Localize("NoPath"), Localize("NoPath"), Localize("OK")); } catch { }
                }
            }
            catch { }
        }

        private async void OnItemHeaderTapped(object sender, EventArgs e)
        {
            try
            {
                // sender will be the StackLayout (header) inside the DataTemplate
                if (sender is View header)
                {
                    // If header has a BindingContext with the loan, select it in the VM so the form fills
                    try
                    {
                        if (header.BindingContext is Biblio_Models.Entiteiten.Lenen lenen && VM != null)
                        {
                            VM.SelectedUitlening = lenen;
                        }
                    }
                    catch { }

                    // Scroll to the edit form so the user sees the populated fields
                    try
                    {
                        // Small delay to let bindings propagate and layout update
                        await System.Threading.Tasks.Task.Delay(80);
                        var sv = this.FindByName<ScrollView>("MainScroll");
                        var form = this.FindByName<VisualElement>("EditForm");
                        if (sv != null && form != null)
                        {
                            await sv.ScrollToAsync(form, ScrollToPosition.MakeVisible, true);
                        }
                    }
                    catch { }

                    // Find the Frame/DataTemplate parent by climbing up the visual tree
                    Element current = header;
                    while (current != null && !(current is Frame))
                    {
                        current = current.Parent;
                    }

                    if (current is Frame frame)
                    {
                        // The details StackLayout is named 'Details' inside the DataTemplate
                        var details = frame.FindByName<StackLayout>("Details");
                        if (details != null)
                        {
                            details.IsVisible = !details.IsVisible;
                        }
                    }
                }
            }
            catch { }
        }

        private async void OnViewLoanClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Lenen lenen)
                {
                    var boek = lenen.Boek?.Titel ?? "-";
                    var lid = (lenen.Lid?.Voornaam ?? "") + " " + (lenen.Lid?.AchterNaam ?? "");
                    var start = lenen.StartDate.ToString("dd-MM-yyyy");
                    var due = lenen.DueDate.ToString("dd-MM-yyyy");
                    var returned = lenen.ReturnedAt.HasValue ? lenen.ReturnedAt.Value.ToString("dd-MM-yyyy") : Localize("NoPath");

                    // Use ViewModel-provided localized label when available, fallback to hard-coded Dutch
                    var returnedLabel = VM?.ReturnedLabel;
                    if (string.IsNullOrWhiteSpace(returnedLabel) || string.Equals(returnedLabel, "ReturnedLabel", StringComparison.OrdinalIgnoreCase))
                        returnedLabel = "Lever status";

                    var body = $"{lid}\n{boek}\n{Localize("StartLabel")} {start} - {Localize("DueLabel")} {due}\n{returnedLabel} {returned}";
                    await DisplayAlert(Localize("View"), body, Localize("OK"));
                }
            }
            catch { }
        }

        // New handler for tapping the header (names) to show same details as details button
        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            try
            {
                // sender is the StackLayout inside DataTemplate; its BindingContext is the Lenen item
                if (sender is VisualElement el && el.BindingContext is Biblio_Models.Entiteiten.Lenen lenen)
                {
                    var boek = lenen.Boek?.Titel ?? "-";
                    var lid = (lenen.Lid?.Voornaam ?? "") + " " + (lenen.Lid?.AchterNaam ?? "");
                    var start = lenen.StartDate.ToString("dd-MM-yyyy");
                    var due = lenen.DueDate.ToString("dd-MM-yyyy");
                    var returned = lenen.ReturnedAt.HasValue ? lenen.ReturnedAt.Value.ToString("dd-MM-yyyy") : Localize("NoPath");

                    var returnedLabel = VM?.ReturnedLabel;
                    if (string.IsNullOrWhiteSpace(returnedLabel) || string.Equals(returnedLabel, "ReturnedLabel", StringComparison.OrdinalIgnoreCase))
                        returnedLabel = "Lever status";

                    var body = $"{lid}\n{boek}\n{Localize("StartLabel")} {start} - {Localize("DueLabel")} {due}\n{returnedLabel} {returned}";
                    await DisplayAlert(Localize("View"), body, Localize("OK"));
                }
            }
            catch { }
        }

        private async void OnReturnStatusChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Picker picker && picker.SelectedItem is string sel)
                {
                    try
                    {
                        var vm = VM;
                        if (vm != null && vm.SelectedUitlening != null)
                        {
                            vm.SelectedReturnStatus = sel;

                            try { vm.RefreshSelectedLoanUI(); } catch { }

                            // determine localized option strings (not used for persistence here, VM handles mapping)
                            var optDelivered = Localize("ReturnedOption");
                            var optReturn = Localize("Return");
                            var optLate = Localize("Late");

                            // Update the selected item in the collection so UI updates immediately
                            try
                            {
                                var item = vm.SelectedUitlening;
                                var list = vm.Uitleningen;
                                if (item != null)
                                {
                                    var idx = list.ToList().FindIndex(u => u.Id == item.Id);
                                    if (idx >= 0)
                                    {
                                        try
                                        {
                                            var copy = new Biblio_Models.Entiteiten.Lenen
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

                                            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                                            {
                                                try
                                                {
                                                    list[idx] = copy;
                                                    // keep SelectedUitlening pointing to the new instance so the form stays in sync
                                                    vm.SelectedUitlening = copy;
                                                }
                                                catch { }
                                            });
                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => { try { list.Insert(0, item); } catch { } });
                                    }
                                }

                                try
                                {
                                    var onProp = vm.GetType().GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                    if (onProp != null)
                                    {
                                        var args = new System.ComponentModel.PropertyChangedEventArgs(nameof(vm.DebugInfo));
                                        onProp.Invoke(vm, new object[] { args });
                                    }
                                }
                                catch { }
                            }
                            catch { }

                            try
                            {
                                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    try
                                    {
                                        // Prefer setting SelectedIndex from the ViewModel options so picker displays the exact item instance
                                        try
                                        {
                                            var options = vm.ReturnStatusOptions;
                                            if (options != null)
                                            {
                                                var find = options.ToList().FindIndex(s => string.Equals(s, sel, StringComparison.OrdinalIgnoreCase));
                                                if (find >= 0)
                                                {
                                                    picker.SelectedIndex = find;
                                                }
                                                else
                                                {
                                                    picker.SelectedItem = sel;
                                                }
                                            }
                                            else
                                            {
                                                picker.SelectedItem = sel;
                                            }
                                        }
                                        catch
                                        {
                                            try { picker.SelectedItem = sel; } catch { }
                                        }
                                    }
                                    catch { }

                                    try
                                    {
                                        var lblBtn = this.FindByName<Button>("ReturnStatusLabelButton");
                                        if (lblBtn != null) lblBtn.Text = sel;
                                    }
                                    catch { }
                                });
                            }
                            catch { }

                            // Persist the selection and show toast; wait for DB save to complete
                            try
                            {
                                await vm.SaveSelectedReturnStatusAsync();
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Focus the picker when the selected-status label is tapped
        private void OnSelectedReturnStatusLabelTapped(object sender, EventArgs e)
        {
            try
            {
                var picker = this.FindByName<Picker>("ReturnStatusPicker");
                picker?.Focus();
            }
            catch { }
        }

        private async void OnDeleteLoanClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton btn && btn.BindingContext is Biblio_Models.Entiteiten.Lenen item)
                {
                    var confirm = false;
                    try
                    {
                        confirm = await DisplayAlert(Localize("Delete"), Localize("DeleteConfirmation"), Localize("OK"), Localize("Cancel"));
                    }
                    catch { }

                    if (!confirm) return;

                    try
                    {
                        var vm = VM;
                        if (vm != null)
                        {
                            // Use ViewModel command if available
                            try
                            {
                                if (vm.DeleteCommand != null && vm.DeleteCommand.CanExecute(item))
                                {
                                    await vm.DeleteCommand.ExecuteAsync(item);
                                    // reload data to ensure UI (icons/labels) consistent
                                    try { await vm.EnsureDataLoadedAsync(); } catch { }
                                    return;
                                }
                            }
                            catch { }

                            // fallback: call DeleteAsync via reflection
                            try
                            {
                                var del = vm.GetType().GetMethod("DeleteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                                if (del != null)
                                {
                                    var task = del.Invoke(vm, new object[] { item }) as System.Threading.Tasks.Task;
                                    if (task != null) await task;
                                    try { await vm.EnsureDataLoadedAsync(); } catch { }
                                    return;
                                }
                            }
                            catch { }

                            // ultimate fallback: remove via DB and reload
                            try
                            {
                                var dbFactoryField = vm.GetType().GetField("_dbFactory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                var dbFactory = dbFactoryField?.GetValue(vm) as Microsoft.EntityFrameworkCore.IDbContextFactory<Biblio_Models.Data.BiblioDbContext>;
                                if (dbFactory != null)
                                {
                                    using var ctx = dbFactory.CreateDbContext();
                                    var existing = await ctx.Leningens.FindAsync(item.Id);
                                    if (existing != null)
                                    {
                                        ctx.Leningens.Remove(existing);
                                        await ctx.SaveChangesAsync();
                                    }
                                }

                                try { await vm.EnsureDataLoadedAsync(); } catch { }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private async void OnReturnStatusUnfocused(object sender, FocusEventArgs e)
        {
            try
            {
                if (sender is Picker picker)
                {
                    var sel = picker.SelectedItem as string;
                    if (string.IsNullOrWhiteSpace(sel)) return;

                    // debounce rapid open/close events
                    var now = DateTime.UtcNow;
                    if (now - _lastReturnStatusChange < _returnStatusDebounce)
                    {
                        _lastReturnStatusChange = now;
                        return;
                    }

                    _lastReturnStatusChange = now;

                    var vm = VM;
                    if (vm == null || vm.SelectedUitlening == null) return;

                    try
                    {
                        vm.SelectedReturnStatus = sel;
                        try { vm.RefreshSelectedLoanUI(); } catch { }

                        // show highlight on the button/picker to indicate selection applied
                        try
                        {
                            var orange = Microsoft.Maui.Graphics.Color.FromArgb("#80FFA500"); // semi-transparent orange
                            var brush = new Microsoft.Maui.Controls.SolidColorBrush(orange);
                            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try { ReturnStatusLabelButton.Background = brush; } catch { }
                                try { ReturnStatusPicker.Background = brush; } catch { }
                            });
                        }
                        catch { }

                        // restore picker selection/button text on main thread
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try { picker.SelectedItem = sel; } catch { }
                            try
                            {
                                var lblBtn = this.FindByName<Button>("ReturnStatusLabelButton");
                                if (lblBtn != null) lblBtn.Text = sel;
                            }
                            catch { }
                        });

                        // Persist and wait for DB save, show toast via VM helper
                        try
                        {
                            await vm.SaveSelectedReturnStatusAsync();
                        }
                        catch { }
                    }
                    catch { }
                }
                else if (sender is Button btn)
                {
                    // If unfocused invoked from the label button, ensure VM is in sync with picker
                    try
                    {
                        var statusPicker = this.FindByName<Picker>("ReturnStatusPicker");
                        if (statusPicker != null)
                        {
                            var sel = statusPicker.SelectedItem as string;
                            if (!string.IsNullOrWhiteSpace(sel))
                            {
                                var vm = VM;
                                if (vm != null && vm.SelectedUitlening != null)
                                {
                                    vm.SelectedReturnStatus = sel;
                                    try { vm.RefreshSelectedLoanUI(); } catch { }

                                    // show highlight
                                    try
                                    {
                                        var orange = Microsoft.Maui.Graphics.Color.FromArgb("#80FFA500");
                                        var brush = new Microsoft.Maui.Controls.SolidColorBrush(orange);
                                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                                        {
                                            try { ReturnStatusLabelButton.Background = brush; } catch { }
                                            try { ReturnStatusPicker.Background = brush; } catch { }
                                        });
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private async void OnReturnStatusIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Picker picker && picker.SelectedItem is string sel)
                {
                    // treat same as unfocused: update VM and refresh UI
                    var now = DateTime.UtcNow;
                    if (now - _lastReturnStatusChange < _returnStatusDebounce)
                    {
                        _lastReturnStatusChange = now;
                        return;
                    }

                    _lastReturnStatusChange = now;

                    var vm = VM;
                    if (vm == null || vm.SelectedUitlening == null) return;

                    try
                    {
                        vm.SelectedReturnStatus = sel;
                        try { vm.RefreshSelectedLoanUI(); } catch { }

                        // update label button text and highlight
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try { picker.SelectedItem = sel; } catch { }
                            try
                            {
                                var lblBtn = this.FindByName<Button>("ReturnStatusLabelButton");
                                if (lblBtn != null) lblBtn.Text = sel;
                            }
                            catch { }

                            try
                            {
                                var orange = Microsoft.Maui.Graphics.Color.FromArgb("#80FFA500");
                                var brush = new Microsoft.Maui.Controls.SolidColorBrush(orange);
                                try { ReturnStatusLabelButton.Background = brush; } catch { }
                                try { ReturnStatusPicker.Background = brush; } catch { }
                            }
                            catch { }
                        });

                        // Persist change and wait for DB save
                        try
                        {
                            await vm.SaveSelectedReturnStatusAsync();
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
 }