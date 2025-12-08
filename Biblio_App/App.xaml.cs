namespace Biblio_App
{
    public partial class App : Application
    {
        // Use DI to receive the singleton SecurityViewModel and start the app on Shell
        public App(ViewModels.SecurityViewModel security)
        {
            InitializeComponent();
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

        protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}