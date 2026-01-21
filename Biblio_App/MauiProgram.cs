using Biblio_App.Pages;
using Biblio_App.Services;
using Biblio_App.ViewModels;
using Biblio_Models.Data;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Security;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System.Text;

namespace Biblio_App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // App startpunt (vergelijkbaar met Program.cs in een console app).
        // Hier configureren we:
        // - cultuur/language
        // - appsettings.json
        // - MAUI (fonts/toolkit)
        // - dependency injection (services, viewmodels, pages)
        // - HttpClient(s) voor de API
        // - logging

        // Application Insights in de MAUI client uitzetten:
        // dit voorkomt soms TaskCanceledException/errors tijdens afsluiten op bepaalde platformen.
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", "");
        
        var builder = MauiApp.CreateBuilder();

        // 1) Cultuur instellen (voor vertalingen, datumnotatie, ...)
        ConfigureCulture();
        // 2) Config inladen uit appsettings.json (en optioneel Development overrides)
        ConfigureConfiguration(builder);
        // 3) MAUI zelf configureren (fonts/toolkit)
        ConfigureMaui(builder);
        // 4) Dependency Injection: services/viewmodels/pages registreren
        ConfigureServices(builder);
        // 5) HttpClient(s) instellen (API base address, timeouts, certificaten)
        ConfigureHttpClient(builder);
        // 6) Logging instellen
        ConfigureLogging(builder);

        var app = builder.Build();

        // Start database migraties/seed in de achtergrond zodat de app UI snel opent
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
        // We laden hier de configuratie die o.a. de API base-url bevat.
        // Eerst de algemene `appsettings.json`, daarna (optioneel) `appsettings.Development.json`.
        // Dit is handig voor emulator/dev situaties (bv. Android emulator gebruikt meestal 10.0.2.2).
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
        // Registreren van de app + community toolkit + fonts
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
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
        // make MauiDbContext available to MAUI-only code (optional, used by client helpers)
        // The LocalDbContext factory is still registered below for shared model usage.
        services.AddDbContextFactory<Biblio_App.Data.MauiDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(FileSystem.AppDataDirectory, "bibliodatabase.db")}"
        ));

        // ---- Core / App State
        // Taal/vertalingen + beveiligingsstatus (ingelogd of niet)
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<SecurityViewModel>();

        // ---- Database
        // SQLite bestand in AppDataDirectory (platform-onafhankelijk pad)
        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "bibliodatabase.db"
        );

        services.AddDbContextFactory<LocalDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}", o => o.MigrationsAssembly("Biblio_Models"))
        );

        services.AddScoped<ILocalRepository, LocalRepository>();
        services.AddScoped<IGegevensProvider, EfGegevensProvider>();

        // ---- API & Synchronisatie (RESTful API calls naar Biblio_Web)
        // DataSyncService doet "sync alles" + offline-first opslag
        services.AddScoped<IDataSyncService, DataSyncService>();
        services.AddScoped<ILedenService, LedenService>();
        services.AddScoped<IBoekService, BoekService>();
        services.AddScoped<IUitleningenService, UitleningenService>();

        // ---- ViewModels
        // ViewModels worden meestal Transient gemaakt (nieuwe instantie per pagina)
        
        // MainViewModel met lambda factory:
        // we injecteren navigatie-acties (GoToAsync) zodat de ViewModel geen directe UI-referenties nodig heeft.
        services.AddTransient<MainViewModel>(sp =>
            new MainViewModel(
                gegevensProvider: sp.GetService<IGegevensProvider>(),
                openBoeken: () => Shell.Current?.GoToAsync(nameof(BoekenPagina)),
                openLeden: () => Shell.Current?.GoToAsync(nameof(LedenPagina)),
                openUitleningen: () => Shell.Current?.GoToAsync(nameof(UitleningenPagina)),
                openCategorieen: () => Shell.Current?.GoToAsync(nameof(CategorieenPagina)),
                dataSync: sp.GetService<IDataSyncService>()
            ));
        
        services.AddTransient<BoekenViewModel>();
        services.AddTransient<LedenViewModel>();
        services.AddTransient<UitleningenViewModel>();
        services.AddTransient<CategorieenViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<ProfilePageViewModel>();
        services.AddTransient<InstellingenViewModel>();

        // ---- Pages
        // Pagina's ook via DI zodat constructor-injectie (ViewModel) werkt
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
        // API base address uit appsettings.json (met fallback)
        var apiBase =
            builder.Configuration["BiblioApi:BaseAddress"]
            ?? "https://localhost:5001/";

        // Voor emulator/device kan localhost niet werken.
        // Deze helper past de base-url aan (bv. localhost -> 10.0.2.2 voor Android emulator)
        apiBase = ResolveApiBaseForDevice(apiBase);

        // Kleine-timeout auth client voor login (geen token handler attached)
        builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // API client voor de echte REST-calls (Boeken/Leden/Uitleningen/...)
        // Token wordt per request toegevoegd in de services.
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
                    // Dev-only: accepteer self-signed certificaten op localhost/10.0.2.2.
                    // In productie laten we enkel geldige certificaten toe.
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
        // Fire-and-forget: we blokkeren de app-start niet.
        // Dit doet in de achtergrond:
        // - database migraties (schema up-to-date brengen)
        // - seed data (demo data) als de DB leeg is
        _ = Task.Run(async () =>
        {
            try
            {

                // Determine DB path up-front so we can optionally delete it before creating a DbContext.
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "bibliodatabase.db");

                // Allow forcing a full recreate of the local SQLite database from the app.
                try
                {
                    var forceRecreate = Preferences.Default.Get("biblio-recreate-db", false);
                    if (forceRecreate)
                    {
                        try
                        {
                            if (File.Exists(dbPath)) File.Delete(dbPath);
                            var wal = dbPath + "-wal";
                            var shm = dbPath + "-shm";
                            if (File.Exists(wal)) File.Delete(wal);
                            if (File.Exists(shm)) File.Delete(shm);
                            System.Diagnostics.Debug.WriteLine("[INIT] Force-recreate: deleted local DB files");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[INIT] Force-recreate delete failed: {ex.Message}");
                        }

                        // Clear the flag so recreate runs only once unless explicitly set again
                        Preferences.Default.Remove("biblio-recreate-db");
                    }
                }
                catch { }

                using var scope = app.Services.CreateScope();

                var dbFactory = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<LocalDbContext>>();

                using var db = dbFactory.CreateDbContext();

                // Allow forcing a full recreate of the local SQLite database from the app.
                // Usage: set the MAUI preference key "biblio-recreate-db" to true (e.g. via a debug menu
                // or from an external script calling into the app). The app will delete the file and
                // associated -wal/-shm files and then recreate/migrate + seed the database.
                try
                {
                    var forceRecreate = Preferences.Default.Get("biblio-recreate-db", false);
                    if (forceRecreate)
                    {
                        try
                        {
                            if (File.Exists(dbPath)) File.Delete(dbPath);
                            var wal = dbPath + "-wal";
                            var shm = dbPath + "-shm";
                            if (File.Exists(wal)) File.Delete(wal);
                            if (File.Exists(shm)) File.Delete(shm);
                            System.Diagnostics.Debug.WriteLine("[INIT] Force-recreate: deleted local DB files");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[INIT] Force-recreate delete failed: {ex.Message}");
                        }

                        // Clear the flag so recreate runs only once unless explicitly set again
                        Preferences.Default.Remove("biblio-recreate-db");
                    }
                }
                catch { }

                // Diagnostics (writes to same log file used elsewhere in the app)
                try
                {
                    var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                    var conn = "(unknown)";
                    try { conn = db.Database.GetDbConnection()?.ConnectionString ?? conn; } catch { }
                    var size = 0L;
                    try { if (File.Exists(dbPath)) size = new FileInfo(dbPath).Length; } catch { }
                    File.AppendAllText(logPath,
                        $"[{DateTime.UtcNow:O}] [INIT] AppDataDirectory={FileSystem.AppDataDirectory} dbPath={dbPath} size={size} conn={conn}\n",
                        Encoding.UTF8);
                }
                catch { }

                // Only migrate if necessary - check if database exists first
                var dbExists = await db.Database.CanConnectAsync();
                if (!dbExists || (await db.Database.GetPendingMigrationsAsync()).Any())
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Running database migrations...");
                    try
                    {
                        await db.Database.MigrateAsync();
                        try
                        {
                            var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                            var size = 0L;
                            try { if (File.Exists(dbPath)) size = new FileInfo(dbPath).Length; } catch { }
                            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] MigrateAsync OK size={size}\n", Encoding.UTF8);
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] MigrateAsync FAILED: {ex.GetType().Name}: {ex.Message}\n", Encoding.UTF8);
                        }
                        catch { }
                        throw;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Database already up to date, skipping migrations");
                }

                // Always try to seed - SeedData.SeedAsync checks internally whether data already exists
                System.Diagnostics.Debug.WriteLine("[INIT] Seeding database...");
                try
                {
                    await Biblio_Models.Seed.SeedData.SeedAsync(db, new Biblio_Models.Seed.SeedOptions
                    {
                        NumberOfBooks = 20,
                        NumberOfMembers = 10
                    });

                    try
                    {
                        var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                        var count = 0;
                        try { count = await db.Boeken.CountAsync(); } catch { }
                        File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] SeedAsync OK boeken={count}\n", Encoding.UTF8);
                    }
                    catch { }

                    System.Diagnostics.Debug.WriteLine("[INIT] Database seeded successfully.");
                }
                catch (SqliteException sx)
                {
                    // Common cause: pushed/corrupt DB or missing tables in the SQLite file.
                    // Attempt to repair by deleting the local DB file and recreating + seeding.
                    try
                    {
                        var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                        File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] SeedAsync SqliteException: {sx.Message}\n", Encoding.UTF8);
                    }
                    catch { }

                    try
                    {
                        // Delete local DB files and recreate schema
                        if (File.Exists(dbPath)) File.Delete(dbPath);
                        var wal = dbPath + "-wal";
                        var shm = dbPath + "-shm";
                        if (File.Exists(wal)) File.Delete(wal);
                        if (File.Exists(shm)) File.Delete(shm);

                        System.Diagnostics.Debug.WriteLine("[INIT] Sqlite error detected - deleted local DB, recreating and seeding...");

                        using var recreatedDb = dbFactory.CreateDbContext();
                        await recreatedDb.Database.EnsureCreatedAsync();
                        await Biblio_Models.Seed.SeedData.SeedAsync(recreatedDb, new Biblio_Models.Seed.SeedOptions
                        {
                            NumberOfBooks = 20,
                            NumberOfMembers = 10
                        });

                        try
                        {
                            var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                            var count = 0;
                            try { count = await recreatedDb.Boeken.CountAsync(); } catch { }
                            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] Repair SeedAsync OK boeken={count}\n", Encoding.UTF8);
                        }
                        catch { }

                        System.Diagnostics.Debug.WriteLine("[INIT] Recreated and seeded local DB successfully.");
                    }
                    catch (Exception rex)
                    {
                        try
                        {
                            var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] Repair failed: {rex.GetType().Name}: {rex.Message}\n", Encoding.UTF8);
                        }
                        catch { }
                        System.Diagnostics.Debug.WriteLine($"[INIT] Repair failed: {rex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        var logPath = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                        File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] [INIT] SeedAsync FAILED: {ex.GetType().Name}: {ex.Message}\n", Encoding.UTF8);
                    }
                    catch { }
                    System.Diagnostics.Debug.WriteLine($"[INIT] Seed failed: {ex.Message}");
                }

                // Sync disabled - app works fully offline with local SQLite data
                // To enable: uncomment below and ensure Biblio_Web API is running
                /*
                var sync = scope.ServiceProvider.GetService<IDataSyncService>();
                if (sync != null)
                {
                    System.Diagnostics.Debug.WriteLine("[INIT] Starting background sync...");
                    await sync.SyncAllAsync();
                    System.Diagnostics.Debug.WriteLine("[INIT] Background sync completed");
                }
                */
                System.Diagnostics.Debug.WriteLine("[INIT] Sync disabled - app running in offline mode");
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
