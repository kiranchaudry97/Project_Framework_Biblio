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

namespace Biblio_App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
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
        builder.Configuration.AddJsonFile(
            "appsettings.json",
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

        services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}")
        );

        services.AddScoped<EfGegevensProvider>();
        services.AddScoped<IGegevensProvider>(sp =>
            sp.GetRequiredService<EfGegevensProvider>());

        services.AddScoped<ILocalRepository, LocalRepository>();

        // ---- API & Synchronisatie
        services.AddScoped<IDataSyncService, DataSyncService>();
        services.AddScoped<ILedenService, LedenService>();
        services.AddScoped<IBoekService, BoekService>();
        services.AddScoped<IUitleningenService, UitleningenService>();

        services.AddSingleton<Synchronizer>();

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

        builder.Services.AddHttpClient("ApiWithToken", client =>
        {
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) =>
                        message.RequestUri?.Host == "localhost"
                        || errors == SslPolicyErrors.None
            });
    }

    // ================= LOGGING =================

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
#if DEBUG
        builder.Logging.AddDebug();
#endif
    }

    // ================= BACKGROUND INIT =================

    private static void StartBackgroundInitialization(MauiApp app)
    {
        Task.Run(async () =>
        {
            using var scope = app.Services.CreateScope();

            var dbFactory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<LocalDbContext>>();

            using var db = dbFactory.CreateDbContext();

            await db.Database.MigrateAsync();
            await LocalDbContext.SeedAsync(db);

            var sync = scope.ServiceProvider
                .GetService<IDataSyncService>();

            if (sync != null)
                await sync.SyncAllAsync();
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
