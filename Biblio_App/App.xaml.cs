namespace Biblio_App
{
    public partial class App : Application
    {
        private readonly ViewModels.SecurityViewModel _security;

        // Dit is de MAUI Application class.
        // Hier maken we de startpagina (Shell) en zetten we globale exception logging.
        // We gebruiken DI om de singleton `SecurityViewModel` te krijgen (login status).
        public App(ViewModels.SecurityViewModel security)
        {
            InitializeComponent();
            
            _security = security;
            
            // Start met Shell als hoofd-navigatiecontainer (menu + routes)
            MainPage = new AppShell();

            var current = SynchronizationContext.Current;
            // Only wrap an existing SynchronizationContext. Replacing a null context with
            // a SafeSynchronizationContext that uses the thread-pool can change dispatch
            // semantics and lead to deadlocks/ANR on some platforms (Android). Skip setting
            // the proxy when there is no inner context.
            if (current != null)
            {
                // SafeSynchronizationContext beschermt tegen bepaalde crashes/deadlocks
                // door dispatching veiliger af te handelen.
                SynchronizationContext.SetSynchronizationContext(new Infrastructure.SafeSynchronizationContext(current));
            }

            // Globale exception handlers:
            // - zorgen dat onverwachte fouten gelogd worden i.p.v. "stil" te crashen.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try 
                { 
                    var ex = e.ExceptionObject as Exception;
                    System.Diagnostics.Debug.WriteLine($"[UNHANDLED EXCEPTION] {ex?.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex?.StackTrace}");
                    Infrastructure.ErrorLogger.Log(ex); 
                } 
                catch { }
            };

            // DEVELOPMENT HELPER: forceer éénmalig recreatie van de lokale MAUI database.
            // Zet de voorkeur alleen tijdelijk (verwijdert device DB op volgende app start
            // als de flag aanwezig is) zodat de app zelf migraties en seeding uitvoert.
            try
            {
                // Comment out or remove this line after first successful run.
                Microsoft.Maui.Storage.Preferences.Default.Set("biblio-recreate-db", true);
            }
            catch { }

            // Ongeobserveerde Task exceptions (async fouten die niemand awaited)
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try 
                { 
                    System.Diagnostics.Debug.WriteLine($"[UNOBSERVED TASK EXCEPTION] {e.Exception?.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {e.Exception?.StackTrace}");
                    Infrastructure.ErrorLogger.Log(e.Exception); 
                } 
                catch { }
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // In MAUI kan je per platform/venster instellingen doen (titel, grootte, ...)
            var window = base.CreateWindow(activationState);
            
            window.Title = "Biblio App";
            
#if WINDOWS
            // Windows-specific window configuration
            window.MinimumWidth = 1000;
            window.MinimumHeight = 700;
            window.Width = 1280;
            window.Height = 800;
            
            window.Created += (s, e) =>
            {
                // Center window on screen at startup
                var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
                window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
                window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
            };
#elif MACCATALYST
            // macOS-specific window configuration
            window.MinimumWidth = 1000;
            window.MinimumHeight = 700;
            window.Width = 1280;
            window.Height = 800;
#endif

            // Perform initial navigation after a delay to ensure Shell is fully initialized
            // Use Dispatcher instead of window.Created to avoid timing issues
            Dispatcher.Dispatch(async () =>
            {
                // We wachten kort zodat Shell volledig geïnitialiseerd is voor navigatie
                await Task.Delay(250);
                
                try
                {
                    if (_security.IsAuthenticated)
                    {
                        // Gebruiker is ingelogd -> ga rechtstreeks naar het boeken-overzicht
                        System.Diagnostics.Debug.WriteLine("User is authenticated, navigating to BoekenShell");
                        await Shell.Current.GoToAsync("//BoekenShell");
                    }
                    else
                    {
                        // Gebruiker is niet ingelogd -> ga naar login pagina
                        System.Diagnostics.Debug.WriteLine("User is not authenticated, navigating to LoginPage");
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                    // Als navigatie faalt, proberen we minstens een veilige fallback route
                    try
                    {
                        await Shell.Current.GoToAsync("//BoekenShell");
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fallback navigation also failed: {ex2.Message}");
                    }
                }
            });
            
            return window;
        }
    }
}