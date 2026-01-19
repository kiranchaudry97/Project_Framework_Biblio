using Biblio_App.Pages;
using Biblio_App.Services;
using Biblio_App.ViewModels;
using Biblio_Models.Data;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Globalization;
using System.Net.Security;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;

namespace Biblio_App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Disable Application Insights telemetry to prevent TaskCanceledException during shutdown
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", "");
        
        var builder = MauiApp.CreateBuilder();

        ConfigureCulture();
        ConfigureConfiguration(builder);
        ConfigureMaui(builder);
        ConfigureServices(builder);
        ConfigureHttpClient(builder);
        ConfigureLogging(builder);

        var app = builder.Build();

        StartBackgroundInitialization(app);

        return app;
    }

    // ================= CULTUUR =================

    private static void ConfigureCulture()
    {
        const string key = "biblio-culture";

        if (!Preferences.Default.ContainsKey(key))
            return;

        var cultureCode = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(cultureCode))
            return;

        var culture = new CultureInfo(cultureCode);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    // ================= CONFIG =================

    private static void ConfigureConfiguration(MauiAppBuilder builder)
    {
        // Load base settings, then optional development overrides (emulator-friendly).
        builder.Configuration.AddJsonFile(
            "appsettings.json",
            optional: true,
            reloadOnChange: true
        );

        // Optional development file — place emulator-specific overrides here,
        // for example using http://10.0.2.2:5000/ when running the Android emulator.
        builder.Configuration.AddJsonFile(
            "appsettings.Development.json",
            optional: true,
            reloadOnChange: true
        );
    }


    // ================= MAUI =================     

    private static void ConfigureMaui(MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
    }

    // ================= SERVICES =================

    private static void ConfigureServices(MauiAppBuilder builder)
    {
        var services = builder.Services;

        // ---- Core / App State
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<SecurityViewModel>();

        // ---- Database
        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "BiblioApp.db"
        );

        services.AddDbContextFactory<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}")
        );

        services.AddScoped<ILocalRepository, LocalRepository>();
        services.AddScoped<IGegevensProvider, EfGegevensProvider>();

        // ---- API & Synchronisatie (RESTful API calls naar Biblio_Web)
        services.AddScoped<IDataSyncService, DataSyncService>();
        services.AddScoped<ILedenService, LedenService>();
        services.AddScoped<IBoekService, BoekService>();
        services.AddScoped<IUitleningenService, UitleningenService>();

        // ---- ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<BoekenViewModel>();
        services.AddTransient<LedenViewModel>();
        services.AddTransient<UitleningenViewModel>();
        services.AddTransient<CategorieenViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<ProfilePageViewModel>();
        services.AddTransient<InstellingenViewModel>();

        // ---- Pages
        services.AddTransient<MainPage>();
        services.AddTransient<BoekenPagina>();
        services.AddTransient<LedenPagina>();
        services.AddTransient<UitleningenPagina>();
        services.AddTransient<CategorieenPagina>();

        services.AddTransient<Pages.Boek.BoekCreatePage>();

        services.AddTransient<Pages.Account.LoginPage>();
        services.AddTransient<Pages.Account.UsersPage>();
        services.AddTransient<Pages.Account.ProfilePage>();

        services.AddTransient<InstellingenPagina>();
    }

    // ================= HTTP =================

    private static void ConfigureHttpClient(MauiAppBuilder builder)
    {
        var apiBase =
            builder.Configuration["BiblioApi:BaseAddress"]
            ?? "https://localhost:5001/";

        apiBase = ResolveApiBaseForDevice(apiBase);

        // Register a small-timeout auth client used by AuthService (no token handler attached)
        builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // API client for RESTful calls to Biblio_Web (manual token handling in each service)
        builder.Services.AddHttpClient("ApiWithToken", client =>
        {
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromMinutes(2);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    var host = message?.RequestUri?.Host;
                    // Accept self-signed/dev certs for local development hosts (emulator/device)
                    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase))
                    {
#if DEBUG
                        return true;
#else
                        return errors == SslPolicyErrors.None;
#endif
                    }

                    return errors == SslPolicyErrors.None;
                }
            });
    }

    // ================= LOGGING =================

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
#if DEBUG
        builder.Logging.AddDebug();
#endif
        
        // Explicitly disable Application Insights telemetry in MAUI client app
        builder.Logging.AddFilter("Microsoft.ApplicationInsights", LogLevel.None);
    }

    // ================= BACKGROUND INIT =================

    private static void StartBackgroundInitialization(MauiApp app)
    {
        // Fire and forget - don't block app startup
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();

                var dbFactory = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<LocalDbContext>>();

                using var db = dbFactory.CreateDbContext();

                // Only migrate if necessary - check if database exists first
                var dbExists = await db.Database.CanConnectAsync();
                if (!dbExists || (await db.Database.GetPendingMigrationsAsync()).Any())
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Running database migrations...");
                    await db.Database.MigrateAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Database already up to date, skipping migrations");
                }

                // Only seed if database is empty
                var hasData = await db.Boeken.AnyAsync();
                if (!hasData)
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Seeding database...");
                    await LocalDbContext.SeedAsync(db);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Database already seeded, skipping seed");
                }

                // Sync in background without blocking
                var sync = scope.ServiceProvider.GetService<IDataSyncService>();
                if (sync != null)
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Starting background sync...");
                    await sync.SyncAllAsync();
                    System.Diagnostics.Debug.WriteLine("[INIT] Background sync completed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[INIT ERROR] {ex.Message}");
                // Don't crash the app if background init fails
            }
        });
    }

    // ================= HELPERS =================

    private static string ResolveApiBaseForDevice(string apiBase)
    {
        if (DeviceInfo.Platform == DevicePlatform.Android &&
            apiBase.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return apiBase.Replace("localhost", "10.0.2.2");
        }

        return apiBase;
    }
}
