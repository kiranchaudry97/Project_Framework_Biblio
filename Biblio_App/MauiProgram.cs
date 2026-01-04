using Biblio_App.Pages;
using Biblio_App.Services;
using Biblio_App.ViewModels;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using CommunityToolkit.Maui;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // configuratie-extensies
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;

namespace Biblio_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

#if DEBUG
// TIJDELIJK UITGESCHAKELD: Database verwijderen voorkomt dat seeding wordt uitgevoerd
// Uncommentarieer indien je de database handmatig moet resetten
/*
            try
            {
                if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.WinUI)
                {
                    Biblio_App.Dev.DevHelpers.DeleteLocalDbIfExists();
                }
            }
            catch { }
            */
#endif

// Pas opgeslagen taalvoorkeur toe indien beschikbaar, anders gebruik apparaat/systeem cultuur
try
            {
                const string prefKey = "biblio-culture";
                if (Preferences.Default.ContainsKey(prefKey))
                {
                    var code = Preferences.Default.Get(prefKey, string.Empty);
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        var culture = new CultureInfo(code);
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;
                        try { Biblio_Models.Resources.SharedModelResource.Culture = culture; } catch { }
                    }
                }
            }
            catch { }

            // Laad optionele appsettings.json zodat het API-basisadres of de connectiestring zonder codewijzigingen geconfigureerd kan worden
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // Registreer language service die ook opgeslagen cultuur toepast in ctor
            builder.Services.AddSingleton<ILanguageService, LanguageService>();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Bepaal API-basisadres uit configuratie (ondersteunt 'BiblioApi:BaseAddress')
            var apiBase = builder.Configuration.GetSection("BiblioApi")["BaseAddress"] ?? builder.Configuration["ApiBaseAddress"] ?? "https://localhost:5001/";

            // Normaliseer ApiBase voor emulator/apparaat scenario's (vervang localhost voor Android emulators etc.)
            apiBase = ResolveApiBaseForDevice(apiBase);

            try
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Resolved ApiBase = '{apiBase}'");
            }
            catch { }

            // Registreer pagina's
            builder.Services.AddTransient<MainPage>();

            // Registreer een singleton SecurityViewModel om de loginstatus in de app te bewaren
            builder.Services.AddSingleton<SecurityViewModel>();

            // Registreer nieuw aangemaakte pagina's
            // De Users-pagina bevindt zich in de Account-namespace
            builder.Services.AddTransient<Pages.Account.UsersPage>();

            // Registreer UsersViewModel
            builder.Services.AddTransient<ViewModels.UsersViewModel>();

            // Configureer DbContextFactory:
            // Gebruik lokale SQLite bestand voor alle platforms (zorg ervoor dat MAUI app dezelfde naam gebruikt als LocalDbContext fallback)
            string dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "BiblioApp.db");

            // Log DB pad en AppDataDirectory zodat het gemakkelijk te vinden is op Windows
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] DB path: {dbPath}");
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] AppDataDirectory: {FileSystem.AppDataDirectory}");
                // Schrijf ook een klein marker bestand in de app data directory zodat je het kunt openen vanuit Explorer
                try
                {
                    var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_paths.log");
                    File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] DB: {dbPath}\nApiBase: {apiBase}\n");
                }
                catch { }
            }
            catch { }

            builder.Services.AddDbContextFactory<Biblio_Models.Data.LocalDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddDbContext<Biblio_Models.Data.LocalDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Registreer EF-gebaseerde gegevensprovider (factory-gebaseerd)
            builder.Services.AddScoped<EfGegevensProvider>();
            builder.Services.AddScoped<IGegevensProvider>(sp => sp.GetRequiredService<EfGegevensProvider>());

            // Eenvoudige HttpClient configuratie zonder TokenHandler. Services voegen zelf de Authorization-header toe per request.
            builder.Services.AddHttpClient("ApiWithToken", c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    // Alleen ontwikkeling: accepteer zelf-ondertekende certificaten voor localhost. Verwijder in productie.
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        if (message.RequestUri?.Host == "localhost") return true;
                        return errors == SslPolicyErrors.None;
                    }
                });

            builder.Services.AddScoped<IDataSyncService, DataSyncService>();
            builder.Services.AddScoped<ILocalRepository, LocalRepository>();
            // Eenvoudige API services (HttpClientFactory + Bearer header uit SecureStorage)
            builder.Services.AddScoped<ILedenService, LedenService>();
            builder.Services.AddScoped<IBoekService, BoekService>();
            builder.Services.AddScoped<IUitleningenService, UitleningenService>();

            // MainViewModel transient (is afhankelijk van services via factory)
            builder.Services.AddTransient<MainViewModel>(sp =>
                new MainViewModel(
                    gegevensProvider: sp.GetService<IGegevensProvider>(),
                    openBoeken: () => Shell.Current?.GoToAsync(nameof(BoekenPagina)),
                    openLeden: () => Shell.Current?.GoToAsync(nameof(LedenPagina)),
                    openUitleningen: () => Shell.Current?.GoToAsync(nameof(UitleningenPagina)),
                    openCategorieen: () => Shell.Current?.GoToAsync(nameof(CategorieenPagina)),
                    dataSync: sp.GetService<IDataSyncService>()
                ));

            // Registreer pagina's en viewmodels als transient en gebruik factory waar nodig
            builder.Services.AddTransient<BoekenPagina>();
            builder.Services.AddTransient<BoekenViewModel>(sp => ActivatorUtilities.CreateInstance<BoekenViewModel>(sp));

            // Maak pagina voor het toevoegen van een nieuw boek (namespace onder Pages.Boek)
            builder.Services.AddTransient<Biblio_App.Pages.Boek.BoekCreatePage>();

            builder.Services.AddTransient<LedenPagina>();
            builder.Services.AddTransient<LedenViewModel>(sp => ActivatorUtilities.CreateInstance<LedenViewModel>(sp));

            builder.Services.AddTransient<UitleningenPagina>();
            builder.Services.AddTransient<UitleningenViewModel>(sp => ActivatorUtilities.CreateInstance<UitleningenViewModel>(sp));

            builder.Services.AddTransient<CategorieenPagina>();
            builder.Services.AddTransient<CategorieenViewModel>(sp => ActivatorUtilities.CreateInstance<CategorieenViewModel>(sp));

            // Login-pagina - biedt een eenvoudige native login-UI in de MAUI-app
            builder.Services.AddTransient<Pages.Account.LoginPage>();

            // Instellingen-pagina/viewmodel
            builder.Services.AddTransient<InstellingenPagina>();
            builder.Services.AddTransient<InstellingenViewModel>(sp => ActivatorUtilities.CreateInstance<InstellingenViewModel>(sp));

            // Profielpagina (lichtgewicht) - maak aan als gebruiker een native profielweergave wil
            builder.Services.AddTransient<Pages.Account.ProfilePage>();

            // Registreer ProfilePageViewModel als transient
            builder.Services.AddTransient<ViewModels.ProfilePageViewModel>();

            // Registreer Synchronizer als singleton en maak LocalDbContext via factory
            builder.Services.AddSingleton<Synchronizer>(sp =>
            {
                var dbFactory = sp.GetService<IDbContextFactory<Biblio_Models.Data.LocalDbContext>>();
                Biblio_Models.Data.LocalDbContext? ctx = null;
                try { ctx = dbFactory?.CreateDbContext(); } catch { ctx = null; }
                var cfg = sp.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                var apiBase = cfg?["ApiBaseAddress"] ?? cfg?.GetSection("Api")?["BaseAddress"];

                // Gebruik IHttpClientFactory om de benoemde client aan te maken die TokenHandler heeft bevestigd
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var client = httpFactory.CreateClient("ApiWithToken");

                return new Synchronizer(ctx ?? throw new InvalidOperationException("LocalDbContext factory not available"), client, apiBase);
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Start achtergrond initialisatie/synchronisatie (fire-and-forget)
            try
            {
                var sync = app.Services.GetService<Synchronizer>();
                if (sync != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await sync.InitializeDb();
                            await sync.SynchronizeAll();
                        }
                        catch (Exception ex)
                        {
                            try { System.Diagnostics.Debug.WriteLine($"Synchronizer background task error: {ex}"); } catch { }
                        }
                    });
                }
            }
            catch { }

            // Zorg dat de database is aangemaakt, migraties toegepast en minimale seed-data ingevoegd bij eerste start.
            // Draai dit op de achtergrond om te voorkomen dat de UI-thread geblokkeerd wordt (voorkomt ANR op Android emulators/devices).
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await InitializeDatabaseAsync(app.Services);
                    }
                    catch (Exception ex)
                    {
                        var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("MauiProgram");
                        logger?.LogError(ex, "Database initialization failed.");
                        try { Infrastructure.ErrorLogger.Log(ex); } catch { }

                        // schrijf fout-marker zodat we dit op device/emulator kunnen onderzoeken
                        try
                        {
                            var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                            File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Database initialization failed: {ex}\n");
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("MauiProgram");
                logger?.LogError(ex, "Database initialization scheduling failed.");
                try { Infrastructure.ErrorLogger.Log(ex); } catch { }
            }

            // Fire-and-forget initiële synchronisatie op de achtergrond om het opstarten niet te blokkeren
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = app.Services.CreateScope();
                        var sync = scope.ServiceProvider.GetService<IDataSyncService>();
                        if (sync != null)
                        {
                            await sync.SyncAllAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Infrastructure.ErrorLogger.Log(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Infrastructure.ErrorLogger.Log(ex);
            }

            return app;
        }

        private static string ResolveApiBaseForDevice(string apiBase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiBase)) return apiBase;
                var uri = new Uri(apiBase);
                var host = uri.Host;

                // Android emulator: use 10.0.2.2 to reach host machine
                if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
                {
                    if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.Equals("127.0.0.1"))
                    {
                        var builder = new UriBuilder(uri)
                        {
                            Host = "10.0.2.2"
                        };
                        return builder.Uri.ToString();
                    }
                }

                // iOS simulator/device: you may need machine IP; keep localhost for Mac Catalyst, adjust for iOS if needed
                if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.iOS)
                {
                    // If using localhost with iOS devices, consider replacing with local network IP
                    // Leaving as-is unless explicitly configured
                    return apiBase;
                }

                return apiBase;
            }
            catch
            {
                return apiBase;
            }
        }

        private static async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");

            using var scope = services.CreateScope();

            async Task EnsureForContextAsync<TContext>() where TContext : DbContext
            {
                try
                {
                    var dbFactory = scope.ServiceProvider.GetService<IDbContextFactory<TContext>>();
                    TContext? ctx = dbFactory != null ? dbFactory.CreateDbContext() : scope.ServiceProvider.GetService<TContext>();
                    if (ctx == null) return;

                    var db = ctx.Database;
                    var provider = db.ProviderName ?? string.Empty;

                    try
                    {
                        var pending = db.GetPendingMigrations();
                        if (pending != null && pending.Any())
                        {
                            await db.MigrateAsync();
                            try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Applied migrations for {typeof(TContext).Name}\n"); } catch { }
                            return;
                        }
                    }
                    catch { /* als GetPendingMigrations mislukt, val terug */ }

                    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        var created = await db.EnsureCreatedAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] EnsureCreatedAsync used for {typeof(TContext).Name}, created={created}\n"); } catch { }
                        
                        // Seed initiële data als database leeg is (niet alleen als nieuw aangemaakt)
                        // Dit draait op achtergrond thread - raak GEEN UI aan hier
                        if (ctx is Biblio_Models.Data.LocalDbContext localCtx)
                        {
                            try
                            {
                                // Controleer of database leeg is (gebruik try-catch voor het geval tabellen nog niet bestaan)
                                bool needsSeeding = false;
                                try
                                {
                                    needsSeeding = !localCtx.Categorien.Any() && !localCtx.Boeken.Any();
                                }
                                catch
                                {
                                    // Als tabellen niet bestaan, hebben we zeker seeding nodig
                                    needsSeeding = true;
                                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Tables don't exist yet, will seed after ensuring schema...\n"); } catch { }
                                }
                                
                                if (needsSeeding)
                                {
                                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Database is empty, seeding data using LocalDbContext.SeedAsync...\n"); } catch { }
                                    await Biblio_Models.Data.LocalDbContext.SeedAsync(localCtx);
                                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Seeded LocalDbContext with initial data via SeedAsync\n"); } catch { }
                                }
                                else
                                {
                                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Database already contains data, skipping seed\n"); } catch { }
                                }
                            }
                            catch (Exception seedEx)
                            {
                                try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Failed to seed LocalDbContext: {seedEx}\n"); } catch { }
                            }
                        }
                    }
                    else
                    {
                        await db.MigrateAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] MigrateAsync used for {typeof(TContext).Name}\n"); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] InitializeDatabaseAsync ({typeof(TContext).Name}) exception: {ex}\n"); } catch { }
                    throw;
                }
            }

            // Pas alleen toe op LocalDbContext (BiblioDbContext wordt niet gebruikt in MAUI app)
            await EnsureForContextAsync<Biblio_Models.Data.LocalDbContext>();
        }

        // Custom seeding removed in favor of LocalDbContext.SeedAsync for consistency across projects.

        public static async Task InitializeAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Definieer admin-gegevens en rol
            string adminEmail = "admin@example.com";
            string adminRole = "Admin";

            // Maak admin-rol aan als die niet bestaat
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // Maak standaard admin-gebruiker aan als die niet bestaat
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = adminEmail, Email = adminEmail };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, adminRole);
            }
            else
            {
                // Reset wachtwoord als admin al bestaat
                var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                await userManager.ResetPasswordAsync(admin, token, "Admin123!");
            }
        }

        
    }
}
