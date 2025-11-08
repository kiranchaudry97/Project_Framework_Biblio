// Doel: Bootstrap van WPF-app met generieke host, DI, EF Core, Identity en seeding.
// Beschrijving: Start de Host, configureert services (DbContext, Identity, ViewModels/Windows),
// voert database seeding uit en toont het hoofdvenster. Bevat globale exception handlers.
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Biblio_Models.Seed;
using Biblio_WPF.ViewModels;
using Biblio_WPF.Window;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Biblio_WPF
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; } = null!;
        private static readonly string ThemeFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Biblio", "theme.txt");

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddUserSecrets<App>(optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Database
                    var connString = context.Configuration.GetConnectionString("DefaultConnection")
                                     ?? "Server=(localdb)\\MSSQLLocalDB;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";
                    services.AddDbContext<BiblioDbContext>(options => options.UseSqlServer(connString));

                    // Identity
                    services.AddIdentityCore<AppUser>(options =>
                    {
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequiredLength = 6;
                    })
                        .AddRoles<IdentityRole>()
                        .AddEntityFrameworkStores<BiblioDbContext>();

                    // Seed options
                    services.AddOptions<Biblio_Models.Seed.SeedOptions>()
                        .Bind(context.Configuration.GetSection("Seed"));

                    // Windows (use types defined in Biblio_WPF.Window)
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<RegisterWindo>();
                    services.AddTransient<WachtwoordVeranderenWindw>();
                    services.AddTransient<ProfileWindow>();
                    services.AddTransient<AdminUsersWindow>();
                    services.AddTransient<BoekWindow>();
                    services.AddTransient<LidWindow>();
                    services.AddTransient<UitleningWindow>();
                    services.AddTransient<CategoriesWindow>();

                    // ViewModels / Services
                    services.AddSingleton<SecurityViewModel>();
                })
                .Build();

            // Global exception handling
            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(args.Exception.Message, "Onverwachte fout", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    MessageBox.Show(ex.Message, "Onverwachte fout (achtergrond)", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Write progress to console so 'dotnet run' shows startup steps in terminal
            try
            {
                Console.WriteLine("[App] Starting host...");
                await AppHost.StartAsync();
                Console.WriteLine("[App] Host started.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[App] Failed to start host: {ex}");
                MessageBox.Show($"Fout tijdens starten Host:\n{ex.Message}", "Initialisatie", MessageBoxButton.OK, MessageBoxImage.Error);
                // If host failed to start we should not continue
                return;
            }

            // register SecurityViewModel instance into application resources for XAML binding
            var securityVm = AppHost.Services.GetRequiredService<SecurityViewModel>();
            Application.Current.Resources["SecurityViewModel"] = securityVm;

            try
            {
                Console.WriteLine("[App] Running database seed/migrations...");
                await SeedData.InitializeAsync(AppHost.Services);
                Console.WriteLine("[App] Database seed/migrations completed.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[App] Fout tijdens database-initialisatie: {ex}");
                MessageBox.Show($"Fout tijdens database-initialisatie:\n{ex.Message}",
                    "Initialisatie", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Load persisted theme (if any)
            try
            {
                var theme = LoadPersistedTheme();
                if (theme == "Dark") UseDarkTheme();
                else UseLightTheme();
            }
            catch { /* swallow theme load errors */ }

            try
            {
                var main = AppHost.Services.GetRequiredService<MainWindow>();
                Console.WriteLine("[App] Showing main window.");
                main.Show();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[App] Fout bij openen hoofdvenster: {ex}");
                MessageBox.Show($"Fout bij openen hoofdvenster:\n{ex.Message}", "Initialisatie", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }

        public static void UseDarkTheme()
        {
            var dict = new ResourceDictionary() { Source = new System.Uri("/Biblio_WPF;component/Styles/Theme.Dark.xaml", System.UriKind.Relative) };
            var light = Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.Light.xaml") == true);
            if (light != null) Current.Resources.MergedDictionaries.Remove(light);
            Current.Resources.MergedDictionaries.Add(dict);
            PersistTheme("Dark");
        }

        public static void UseLightTheme()
        {
            var dict = new ResourceDictionary() { Source = new System.Uri("/Biblio_WPF;component/Styles/Theme.Light.xaml", System.UriKind.Relative) };
            var dark = Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.Dark.xaml") == true);
            if (dark != null) Current.Resources.MergedDictionaries.Remove(dark);
            Current.Resources.MergedDictionaries.Add(dict);
            PersistTheme("Light");
        }

        private static void PersistTheme(string name)
        {
            try
            {
                var dir = Path.GetDirectoryName(ThemeFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                File.WriteAllText(ThemeFilePath, name);
            }
            catch { /* ignore persistence errors */ }
        }

        private static string? LoadPersistedTheme()
        {
            try
            {
                if (File.Exists(ThemeFilePath))
                    return File.ReadAllText(ThemeFilePath).Trim();
            }
            catch { }
            return null;
        }
    }
}
