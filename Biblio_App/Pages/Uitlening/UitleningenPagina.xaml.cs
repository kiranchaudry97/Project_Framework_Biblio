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
        public static readonly BindableProperty DetailsButtonTextProperty = BindableProperty.Create(nameof(DetailsButtonText), typeof(string), typeof(UitleningenPagina), default(string));
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
        public string DetailsButtonText { get => (string)GetValue(DetailsButtonTextProperty); set => SetValue(DetailsButtonTextProperty, value); }
        public string NewButtonText { get => (string)GetValue(NewButtonTextProperty); set => SetValue(NewButtonTextProperty, value); }
        public string SaveButtonText { get => (string)GetValue(SaveButtonTextProperty); set => SetValue(SaveButtonTextProperty, value); }
        public string DeleteButtonText { get => (string)GetValue(DeleteButtonTextProperty); set => SetValue(DeleteButtonTextProperty, value); }

        public UitleningenPagina(UitleningenViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            try
            {
                try { Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false }); } catch { }
                try { Shell.SetFlyoutBehavior(this, FlyoutBehavior.Flyout); } catch { }
                try { NavigationPage.SetHasBackButton(this, false); } catch { }
            }
            catch { }

            try { Resources["PageVM"] = vm; } catch { }

            try { _language_service = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>(); } catch { }

            InitializeSharedResourceManager();
            UpdateLocalizedStrings();

            try
            {
                if (vm != null)
                {
                    vm.PropertyChanged += Vm_PropertyChanged;
                }
            }
            catch { }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (sender is UitleningenViewModel vm)
                {
                    if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(vm.SelectedUitlening) || e.PropertyName == nameof(vm.SelectedReturnStatus) || e.PropertyName == nameof(vm.ReturnStatusOptions))
                    {
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                // ReturnStatusPicker is niet meer in de huidige XAML - skip deze functionaliteit
                                var picker = this.FindByName<Picker>("ReturnStatusPicker");
                                if (picker == null) return; // Geen picker gevonden, skip

                                if (vm.SelectedUitlening == null)
                                {
                                    if (vm.ReturnStatusOptions != null && vm.ReturnStatusOptions.Count > 0)
                                    {
                                        picker.SelectedItem = vm.ReturnStatusOptions.First();
                                    }
                                    else
                                    {
                                        picker.SelectedItem = null;
                                    }
                                    return;
                                }

                                var target = vm.SelectedReturnStatus;
                                if (!string.IsNullOrWhiteSpace(target) && vm.ReturnStatusOptions != null)
                                {
                                    var found = vm.ReturnStatusOptions.FirstOrDefault(s => string.Equals(s, target, StringComparison.OrdinalIgnoreCase));
                                    if (found != null)
                                    {
                                        picker.SelectedItem = found;
                                        return;
                                    }
                                }

                                // Fallback: set to first option if available
                                if (vm.ReturnStatusOptions != null && vm.ReturnStatusOptions.Count > 0)
                                {
                                    picker.SelectedItem = vm.ReturnStatusOptions.First();
                                }
                            }
                            catch { }
                        });
                    }
                }
            }
            catch { }
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
                        "Details" => "Details",
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
                    "Details" => "Details",
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
            DetailsButtonText = Localize("Details");
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

            try
            {
                if (BindingContext is UitleningenViewModel vm) vm.PropertyChanged -= Vm_PropertyChanged;
            }
            catch { }
        }

        private void LanguageService_LanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateLocalizedStrings();
                });
            }
            catch { }
        }

        private async void OnCopyDbPathClicked(object sender, EventArgs e)
        {
            try
            {
                // DbPathLabel is uitgecommentarieerd in XAML - skip functionaliteit
                return;
                
                /*
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
                */
            }
            catch { }
        }

        private async void OnItemHeaderTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is View header)
                {
                    try
                    {
                        if (header.BindingContext is Biblio_Models.Entiteiten.Lenen lenen && VM != null)
                        {
                            VM.SelectedUitlening = lenen;
                        }
                    }
                    catch { }

                    try
                    {
                        await System.Threading.Tasks.Task.Delay(80);
                        var sv = this.FindByName<ScrollView>("MainScroll");
                        var form = this.FindByName<VisualElement>("EditForm");
                        if (sv != null && form != null)
                        {
                            await sv.ScrollToAsync(form, ScrollToPosition.MakeVisible, true);
                        }
                    }
                    catch { }

                    Element current = header;
                    while (current != null && !(current is Frame))
                    {
                        current = current.Parent;
                    }

                    if (current is Frame frame)
                    {
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
                // Visual tree: Border ? Grid ? VerticalStackLayout ? HorizontalStackLayout ? Button
                // So we need to go 4 levels up: Button.Parent.Parent.Parent.Parent = Border
                var button = sender as Button;
                var lenen = button?.Parent?.Parent?.Parent?.Parent?.BindingContext as Biblio_Models.Entiteiten.Lenen;
                
                if (lenen != null)
                {
                    var boek = lenen.Boek?.Titel ?? "-";
                    var lid = (lenen.Lid?.Voornaam ?? "") + " " + (lenen.Lid?.AchterNaam ?? "");
                    var start = lenen.StartDate.ToString("dd-MM-yyyy");
                    var due = lenen.DueDate.ToString("dd-MM-yyyy");
                    var returned = lenen.ReturnedAt.HasValue ? lenen.ReturnedAt.Value.ToString("dd-MM-yyyy") : Localize("NoPath");

                    var startLabel = VM?.StartLabel ?? Localize("StartLabel");
                    var dueLabel = VM?.DueLabel ?? Localize("DueLabel");

                    var returnedLabel = VM?.ReturnedLabel;
                    if (string.IsNullOrWhiteSpace(returnedLabel) || string.Equals(returnedLabel, "ReturnedLabel", StringComparison.OrdinalIgnoreCase))
                        returnedLabel = "Lever status";

                    var body = $"{lid}\n{boek}\n{startLabel} {start} - {dueLabel} {due}\n{returnedLabel} {returned}";
                    await DisplayAlert(Localize("View"), body, Localize("OK"));
                }
            }
            catch { }
        }

        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is VisualElement el && el.BindingContext is Biblio_Models.Entiteiten.Lenen lenen)
                {
                    var boek = lenen.Boek?.Titel ?? "-";
                    var lid = (lenen.Lid?.Voornaam ?? "") + " " + (lenen.Lid?.AchterNaam ?? "");
                    var start = lenen.StartDate.ToString("dd-MM-yyyy");
                    var due = lenen.DueDate.ToString("dd-MM-yyyy");
                    var returned = lenen.ReturnedAt.HasValue ? lenen.ReturnedAt.Value.ToString("dd-MM-yyyy") : Localize("NoPath");

                    var startLabel = VM?.StartLabel ?? Localize("StartLabel");
                    var dueLabel = VM?.DueLabel ?? Localize("DueLabel");

                    var returnedLabel = VM?.ReturnedLabel;
                    if (string.IsNullOrWhiteSpace(returnedLabel) || string.Equals(returnedLabel, "ReturnedLabel", StringComparison.OrdinalIgnoreCase))
                        returnedLabel = "Lever status";

                    var body = $"{lid}\n{boek}\n{startLabel} {start} - {dueLabel} {due}\n{returnedLabel} {returned}";
                    await DisplayAlert(Localize("View"), body, Localize("OK"));
                }
            }
            catch { }
        }

        private async void OnMarkReturnedClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.BindingContext is Biblio_Models.Entiteiten.Lenen item)
                {
                    var vm = VM;
                    if (vm == null) return;

                    try
                    {
                        // Prefer using the ViewModel's async command when available
                        if (vm.ReturnCommand != null && vm.ReturnCommand.CanExecute(item))
                        {
                            await vm.ReturnCommand.ExecuteAsync(item);
                            return;
                        }
                    }
                    catch { }

                    // Fallback: invoke ReturnAsync via reflection
                    try
                    {
                        var ret = vm.GetType().GetMethod("ReturnAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (ret != null)
                        {
                            var task = ret.Invoke(vm, new object[] { item }) as System.Threading.Tasks.Task;
                            if (task != null) await task;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private async void OnDeleteLoanClicked(object sender, EventArgs e)
        {
            try
            {
                // Visual tree: Border ? Grid ? VerticalStackLayout ? HorizontalStackLayout ? Button
                var button = sender as Button;
                var item = button?.Parent?.Parent?.Parent?.Parent?.BindingContext as Biblio_Models.Entiteiten.Lenen;
                
                if (item != null)
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
    }
}