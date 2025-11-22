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

namespace Biblio_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register pages
            builder.Services.AddTransient<MainPage>();

            // Configure local SQLite DbContextFactory for MAUI (safe for background/async use)
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "biblio.db");
            builder.Services.AddDbContextFactory<BiblioDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Register EF-based gegevensprovider (factory-based)
            builder.Services.AddScoped<EfGegevensProvider>();
            builder.Services.AddScoped<IGegevensProvider>(sp => sp.GetRequiredService<EfGegevensProvider>());

            // MainViewModel transient (depends on services via factory)
            builder.Services.AddTransient<MainViewModel>(sp =>
                new MainViewModel(
                    gegevensProvider: sp.GetService<IGegevensProvider>(),
                    openBoeken: () => Shell.Current?.GoToAsync(nameof(BoekenPagina)),
                    openLeden: () => Shell.Current?.GoToAsync(nameof(LedenPagina)),
                    openUitleningen: () => Shell.Current?.GoToAsync(nameof(UitleningenPagina)),
                    openCategorieen: () => Shell.Current?.GoToAsync(nameof(CategorieenPagina))
                ));

            // Register pages and viewmodels as transient and resolve factory where needed
            builder.Services.AddTransient<BoekenPagina>();
            builder.Services.AddTransient<BoekenViewModel>(sp => new BoekenViewModel(
                sp.GetRequiredService<IDbContextFactory<BiblioDbContext>>().CreateDbContext(),
                sp.GetService<IGegevensProvider>()));

            builder.Services.AddTransient<LedenPagina>();
            builder.Services.AddTransient<LedenViewModel>(sp => new LedenViewModel(
                sp.GetRequiredService<IDbContextFactory<BiblioDbContext>>().CreateDbContext(),
                sp.GetService<IGegevensProvider>()));

            builder.Services.AddTransient<UitleningenPagina>();
            builder.Services.AddTransient<UitleningenViewModel>(sp => new UitleningenViewModel(
                sp.GetRequiredService<IDbContextFactory<BiblioDbContext>>().CreateDbContext()));

            builder.Services.AddTransient<CategorieenPagina>();
            builder.Services.AddTransient<CategorieenViewModel>(sp => new CategorieenViewModel(
                sp.GetService<IGegevensProvider>()));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure DB created, migrations applied and minimal seed data inserted on first run.
            try
            {
                InitializeDatabaseAsync(app.Services).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Log but don't crash the app; developers can inspect the output.
                var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("MauiProgram");
                logger?.LogError(ex, "Database initialization failed.");
            }

            return app;
        }

        private static async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            // Use a scope to access services
            using var scope = services.CreateScope();

            // Prefer factory if registered
            var dbFactory = scope.ServiceProvider.GetService<IDbContextFactory<BiblioDbContext>>();
            if (dbFactory != null)
            {
                using var db = dbFactory.CreateDbContext();
                await db.Database.MigrateAsync();

                // minimal seed (categories, boeken, leden) — safe without Identity dependencies
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
                    var roman = await db.Categorien.FirstAsync(c => c.Naam == "Roman");
                    var jeugd = await db.Categorien.FirstAsync(c => c.Naam == "Jeugd");

                    db.Boeken.AddRange(
                        new Biblio_Models.Entiteiten.Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                        new Biblio_Models.Entiteiten.Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                        new Biblio_Models.Entiteiten.Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id }
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

                return;
            }

            // Fallback: try resolving BiblioDbContext directly (if AddDbContext was used)
            var dbCtx = scope.ServiceProvider.GetService<BiblioDbContext>();
            if (dbCtx != null)
            {
                await dbCtx.Database.MigrateAsync();
                return;
            }

            throw new InvalidOperationException("No suitable DbContext or DbContextFactory found.");
        }

        public static async Task InitializeAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Define admin credentials and role
            string adminEmail = "admin@example.com";
            string adminRole = "Admin";

            // Create admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // Create default admin user if it doesn't exist
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = adminEmail, Email = adminEmail };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, adminRole);
            }
            else
            {
                // Reset password if admin already exists
                var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                await userManager.ResetPasswordAsync(admin, token, "Admin123!");
            }
        }
    }
}
