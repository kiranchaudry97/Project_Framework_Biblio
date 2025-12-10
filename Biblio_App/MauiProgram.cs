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

namespace Biblio_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Laad optionele appsettings.json zodat het API-basisadres of de connectiestring zonder codewijzigingen geconfigureerd kan worden
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // Register language service which also applies saved culture in ctor
            builder.Services.AddSingleton<ILanguageService, LanguageService>();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Bepaal API-basisadres uit configuratie (ondersteunt 'ApiBaseAddress' of 'Api:BaseAddress')
            var apiBase = builder.Configuration["ApiBaseAddress"] ?? builder.Configuration.GetSection("Api")["BaseAddress"] ?? "https://localhost:5001/";

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
            // - Als een DefaultConnection is geconfigureerd EN de app op Windows draait, geef dan de voorkeur aan SQL Server (verbindt met dezelfde DB als de webapp).
            // - Anders (niet-Windows of geen DefaultConnection) gebruik lokaal SQLite zodat mobiele/emulator/device-scenario's werken.
            var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection");
            var isWindows = OperatingSystem.IsWindows();
            if (!string.IsNullOrWhiteSpace(sqlConn) && isWindows)
            {
                // Gebruik SQL Server op Windows
                builder.Services.AddDbContextFactory<BiblioDbContext>(options =>
                    options.UseSqlServer(sqlConn));

                // Registreer ook DbContext voor directe injectie in MAUI-pagina's/services indien nodig
                builder.Services.AddDbContext<BiblioDbContext>(options =>
                    options.UseSqlServer(sqlConn));
            }
            else
            {
                // Gebruik lokaal SQLite op niet-Windows of wanneer geen DefaultConnection is opgegeven
                string dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "biblio.db");
                builder.Services.AddDbContextFactory<BiblioDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                // Registreer ook DbContext voor directe injectie
                builder.Services.AddDbContext<BiblioDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
                
                // Registreer ook een lichte LocalDbContext (uit het gedeelde models-project) voor lokale MAUI-bewerkingen
                builder.Services.AddDbContextFactory<Biblio_Models.Data.LocalDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                builder.Services.AddDbContext<Biblio_Models.Data.LocalDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
            }

            // Registreer EF-gebaseerde gegevensprovider (factory-gebaseerd)
            builder.Services.AddScoped<EfGegevensProvider>();
            builder.Services.AddScoped<IGegevensProvider>(sp => sp.GetRequiredService<EfGegevensProvider>());

            // Registreer TokenHandler en API-auth-client
            builder.Services.AddTransient<TokenHandler>();
            builder.Services.AddHttpClient<IAuthService, AuthService>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
            }).AddHttpMessageHandler<TokenHandler>();

            // Genoemde client gebruikt voor andere API-aanroepen (voegt token toe) - houd de genoemde client zodat DataSyncService deze kan gebruiken via HttpClientFactory
            builder.Services.AddHttpClient("ApiWithToken", c =>
            {
                c.BaseAddress = new Uri(apiBase);
            }).AddHttpMessageHandler<TokenHandler>();

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

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

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
            // maak het marker-pad vroeg aan
            var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");

            // Gebruik een scope om services te benaderen
            using var scope = services.CreateScope();

            try
            {
                // Geef de voorkeur aan de factory als deze geregistreerd is
                var dbFactory = scope.ServiceProvider.GetService<IDbContextFactory<BiblioDbContext>>();
                if (dbFactory != null)
                {
                    using var db = dbFactory.CreateDbContext();
                    var provider = db.Database.ProviderName ?? string.Empty;
                    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        // SQLite op device/emulator: migraties die voor SQL Server zijn gegenereerd kunnen falen — maak de DB vanuit het model aan
                        await db.Database.EnsureCreatedAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] EnsureCreatedAsync gebruikt voor provider: {provider}\n"); } catch { }
                    }
                    else
                    {
                        await db.Database.MigrateAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] MigrateAsync gebruikt voor provider: {provider}\n"); } catch { }
                    }

                    // minimale seed (categorieën, boeken, leden) — veilig zonder Identity-afhankelijkheden
                    if (!await db.Categorien.AnyAsync())
                    {
                        db.Categorien.AddRange(
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Roman" },
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Jeugd" },
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Thriller" },
                            new Biblio_Models.Entiteiten.Categorie { Naam = "Wetenschap" }
                        );
                        await db.SaveChangesAsync();
                    }

                    if (!await db.Boeken.AnyAsync())
                    {
                        // Spiegel seed uit Biblio_Models.Seed.SeedData
                        var roman = await db.Categorien.FirstAsync(c => c.Naam == "Roman");
                        var jeugd = await db.Categorien.FirstAsync(c => c.Naam == "Jeugd");
                        var thriller = await db.Categorien.FirstAsync(c => c.Naam == "Thriller");
                        var wetenschap = await db.Categorien.FirstAsync(c => c.Naam == "Wetenschap");

                        db.Boeken.AddRange(
                            new Biblio_Models.Entiteiten.Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "Pride and Prejudice", Auteur = "Jane Austen", Isbn = "9781503290563", CategorieID = roman.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "To Kill a Mockingbird", Auteur = "Harper Lee", Isbn = "9780061120084", CategorieID = roman.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "Brave New World", Auteur = "Aldous Huxley", Isbn = "9780060850524", CategorieID = roman.Id },

                            new Biblio_Models.Entiteiten.Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "Harry Potter en de Steen der Wijzen", Auteur = "J.K. Rowling", Isbn = "9781408855652", CategorieID = jeugd.Id },

                            new Biblio_Models.Entiteiten.Boek { Titel = "The Girl with the Dragon Tattoo", Auteur = "Stieg Larsson", Isbn = "9780307454546", CategorieID = thriller.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "The Da Vinci Code", Auteur = "Dan Brown", Isbn = "9780307474278", CategorieID = thriller.Id },

                            new Biblio_Models.Entiteiten.Boek { Titel = "A Brief History of Time", Auteur = "Stephen Hawking", Isbn = "9780553380163", CategorieID = wetenschap.Id },
                            new Biblio_Models.Entiteiten.Boek { Titel = "The Selfish Gene", Auteur = "Richard Dawkins", Isbn = "9780192860927", CategorieID = wetenschap.Id }
                        );
                        await db.SaveChangesAsync();
                    }

                    if (!await db.Leden.AnyAsync())
                    {
                        db.Leden.AddRange(
                            new Biblio_Models.Entiteiten.Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com" },
                            new Biblio_Models.Entiteiten.Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com" }
                        );
                        await db.SaveChangesAsync();
                    }

                    // Seed talen (Talen) gelijk aan web-seed
                    if (!await db.Set<Biblio_Models.Entiteiten.Taal>().AnyAsync())
                    {
                        db.Set<Biblio_Models.Entiteiten.Taal>().AddRange(
                            new Biblio_Models.Entiteiten.Taal { Code = "nl", Naam = "Nederlands", IsDefault = true },
                            new Biblio_Models.Entiteiten.Taal { Code = "en", Naam = "English", IsDefault = false }
                        );
                        await db.SaveChangesAsync();
                    }

                    // Seed voorbeeldleningen als er geen bestaan zodat de MAUI-app voorbeeld Uitleningen toont
                    if (!await db.Leningens.AnyAsync())
                    {
                        try
                        {
                            var firstBoek = await db.Boeken.AsNoTracking().FirstOrDefaultAsync();
                            var secondBoek = await db.Boeken.AsNoTracking().Skip(1).FirstOrDefaultAsync();
                            var jan = await db.Leden.AsNoTracking().FirstOrDefaultAsync(l => l.Voornaam == "Jan");
                            var sara = await db.Leden.AsNoTracking().FirstOrDefaultAsync(l => l.Voornaam == "Sara");

                            var loans = new List<Biblio_Models.Entiteiten.Lenen>();
                            if (firstBoek != null && jan != null)
                            {
                                loans.Add(new Biblio_Models.Entiteiten.Lenen
                                {
                                    BoekId = firstBoek.Id,
                                    LidId = jan.Id,
                                    StartDate = DateTime.Now.AddDays(-10),
                                    DueDate = DateTime.Now.AddDays(4),
                                    ReturnedAt = null
                                });
                            }
                            if (secondBoek != null && sara != null)
                            {
                                loans.Add(new Biblio_Models.Entiteiten.Lenen
                                {
                                    BoekId = secondBoek.Id,
                                    LidId = sara.Id,
                                    StartDate = DateTime.Now.AddDays(-30),
                                    DueDate = DateTime.Now.AddDays(-16),
                                    ReturnedAt = DateTime.Now.AddDays(-15) // geretourneerd
                                });
                            }

                            if (loans.Count > 0)
                            {
                                db.Leningens.AddRange(loans);
                                await db.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            // negeer seed-fouten maar log naar marker
                            try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] Seed loans error: {ex}\n"); } catch { }
                        }
                    }

                    // schrijf succes-marker
                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] InitializeDatabaseAsync completed successfully.\n"); } catch { }

                    return;
                }

                // Fallback: probeer BiblioDbContext direct te resolven (als AddDbContext gebruikt is)
                var dbCtx = scope.ServiceProvider.GetService<BiblioDbContext>();
                if (dbCtx != null)
                {
                    var provider = dbCtx.Database.ProviderName ?? string.Empty;
                    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        await dbCtx.Database.EnsureCreatedAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] EnsureCreatedAsync used for provider: {provider} (fallback)\n"); } catch { }
                    }
                    else
                    {
                        await dbCtx.Database.MigrateAsync();
                        try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] MigrateAsync used for provider: {provider} (fallback)\n"); } catch { }
                    }

                    // schrijf succes-marker voor fallback-pad
                    try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] InitializeDatabaseAsync fallback (direct DbContext) completed successfully.\n"); } catch { }

                    return;
                }

                throw new InvalidOperationException("Geen geschikte DbContext of DbContextFactory gevonden.");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] InitializeDatabaseAsync exception: {ex}\n"); } catch { }
                throw;
            }
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
    }
}
