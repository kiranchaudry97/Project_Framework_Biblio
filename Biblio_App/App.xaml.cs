namespace Biblio_App
{
    public partial class App : Application
    {
        private readonly ViewModels.SecurityViewModel _security;

        // Use DI to receive the singleton SecurityViewModel and start the app on Shell
        public App(ViewModels.SecurityViewModel security)
        {
            InitializeComponent();
            
            _security = security;
            
            // Start with the Shell as the main page
            MainPage = new AppShell();

            var current = SynchronizationContext.Current;
            // Only wrap an existing SynchronizationContext. Replacing a null context with
            // a SafeSynchronizationContext that uses the thread-pool can change dispatch
            // semantics and lead to deadlocks/ANR on some platforms (Android). Skip setting
            // the proxy when there is no inner context.
            if (current != null)
            {
                SynchronizationContext.SetSynchronizationContext(new Infrastructure.SafeSynchronizationContext(current));
            }

            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { Infrastructure.ErrorLogger.Log(e.ExceptionObject as Exception); } catch { }
            };

            // Log task scheduler unobserved exceptions
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try { Infrastructure.ErrorLogger.Log(e.Exception); } catch { }
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
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

            // Check authentication and navigate to appropriate page after window is fully created
            window.Created += async (s, e) =>
            {
                // Small delay to ensure Shell is fully initialized
                await Task.Delay(100);
                
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (_security.IsAuthenticated)
                        {
                            // User is logged in -> Navigate to Books page
                            await Shell.Current.GoToAsync("//BoekenShell");
                        }
                        else
                        {
                            // User is NOT logged in -> Navigate to Login page
                            await Shell.Current.GoToAsync("//LoginPage");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                    }
                });
            };
            
            return window;
        }
    }
}