using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Biblio_App.Services;
using Biblio_App.Pages;
using Biblio_App.ViewModels;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using System;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Biblio_App.Models;
using Microsoft.Extensions.Configuration; // configuratie-extensies
using System.IO;
using System.Globalization;
using Microsoft.Maui.Devices;
using CommunityToolkit.Maui;
using System.Net.Http;
using System.Net.Security;
using System.Linq;

namespace Biblio_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

#if DEBUG
            try
            {
                if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.WinUI)
                {
                    Biblio_App.Dev.DevHelpers.DeleteLocalDbIfExists();
                }
            }
            catch { }
#endif

            // Apply saved language preference if available, otherwise use device/system culture
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

            // Register language service which also applies saved culture in ctor
            builder.Services.AddSingleton<ILanguageService, LanguageService>();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Bepaal API-basisadres uit configuratie (ondersteunt 'ApiBaseAddress' of 'Api:BaseAddress')
            var apiBase = builder.Configuration["ApiBaseAddress"] ?? builder.Configuration.GetSection("Api")["BaseAddress"] ?? "https://localhost:5001/";

            // Normalize ApiBase for emulator/device scenarios (replace localhost for Android emulators etc.)
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
            // Use local SQLite file for all platforms (ensure MAUI app uses biblio.db)
            string dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "biblio.db");

            // Log DB path and AppDataDirectory so it's easy to find on Windows
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] DB path: {dbPath}");
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] AppDataDirectory: {FileSystem.AppDataDirectory}");
                // Also write a small marker file into the app data directory so you can open it from Explorer
                try
                {
                    var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_paths.log");
                    File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] DB: {dbPath}\nApiBase: {apiBase}\n");
                }
                catch { }
            }
            catch { }

            builder.Services.AddDbContextFactory<BiblioDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Also register DbContext for direct injection in MAUI pages/services if needed
            builder.Services.AddDbContext<BiblioDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Register also LocalDbContext (shared models project) for local MAUI operations
            builder.Services.AddDbContextFactory<Biblio_Models.Data.LocalDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddDbContext<Biblio_Models.Data.LocalDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
                
            // Registreer EF-gebaseerde gegevensprovider (factory-gebaseerd)
            builder.Services.AddScoped<EfGegevensProvider>();
            builder.Services.AddScoped<IGegevensProvider>(sp => sp.GetRequiredService<EfGegevensProvider>());

            // Read UseAuth setting (default true)
            var useAuth = bool.TryParse(builder.Configuration["UseAuth"], out var ua) ? ua : true;

            // Register TokenHandler and HttpClients conditionally based on UseAuth
            if (useAuth)
            {
                // TokenHandler needs IAuthService available to perform refresh calls
                builder.Services.AddTransient<TokenHandler>(sp => ActivatorUtilities.CreateInstance<TokenHandler>(sp));

                // Auth service client (used by TokenHandler to refresh tokens)
                builder.Services.AddHttpClient<IAuthService, AuthService>(c =>
                {
                    c.BaseAddress = new Uri(apiBase);
                    c.Timeout = TimeSpan.FromSeconds(5);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        // Development-only: accept self-signed certs for localhost. Remove in production.
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                        {
                            if (message.RequestUri?.Host == "localhost") return true;
                            return errors == SslPolicyErrors.None;
                        }
                    });

                // API client with TokenHandler attached
                builder.Services.AddHttpClient("ApiWithToken", c =>
                {
                    c.BaseAddress = new Uri(apiBase);
                    c.Timeout = TimeSpan.FromSeconds(10);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                        {
                            if (message.RequestUri?.Host == "localhost") return true;
                            return errors == SslPolicyErrors.None;
                        }
                    })
                .AddHttpMessageHandler<TokenHandler>();
            }
            else
            {
                // Plain API client without token (development or anonymous endpoints)
                builder.Services.AddHttpClient("ApiWithToken", c =>
                {
                    c.BaseAddress = new Uri(apiBase);
                    c.Timeout = TimeSpan.FromSeconds(10);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                        {
                            if (message.RequestUri?.Host == "localhost") return true;
                            return errors == SslPolicyErrors.None;
                        }
                    });
            }

            builder.Services.AddScoped<IDataSyncService, DataSyncService>();
            builder.Services.AddScoped<ILocalRepository, LocalRepository>();

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

                // Use IHttpClientFactory to create the named client that has TokenHandler attached
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var client = httpFactory.CreateClient("ApiWithToken");

                return new Synchronizer(ctx ?? throw new InvalidOperationException("LocalDbContext factory not available"), client, apiBase);
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Start background initialization/synchronization (fire-and-forget)
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
                    catch { /* if GetPendingMigrations fails, fall back */ }

                    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        await db.EnsureCreatedAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] EnsureCreatedAsync used for {typeof(TContext).Name}\n"); } catch { }
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

            // Apply to primary app DbContext and local/shared LocalDbContext
            await EnsureForContextAsync<BiblioDbContext>();
            await EnsureForContextAsync<Biblio_Models.Data.LocalDbContext>();
        }

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

        private static string ResolveApiBaseForDevice(string apiBase)
        {
            if (string.IsNullOrWhiteSpace(apiBase))
                return apiBase;

            try
            {
                // If running on Android emulator, replace localhost with emulator host loopback.
                // Default Android emulator -> 10.0.2.2, Genymotion -> 10.0.3.2.
                try
                {
                    if (DeviceInfo.Platform == DevicePlatform.Android && apiBase.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        apiBase = apiBase.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch { /* DeviceInfo may not be available in some contexts; swallow */ }

                // Additional: if someone configured 127.0.0.1 explicitly, treat same as localhost
                if (apiBase.Contains("127.0.0.1"))
                {
                    apiBase = apiBase.Replace("127.0.0.1", "10.0.2.2", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { /* best-effort */ }

            return apiBase;
        }
    }
}
