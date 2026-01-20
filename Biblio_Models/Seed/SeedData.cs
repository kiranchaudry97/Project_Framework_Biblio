// SeedData bevat EF Core queries (LINQ) en CRUD-operaties en gebruikt lambda-expressies.
// Dit bestand seedt meerdere "dummy"/test-objecten die door de app en UI worden gebruikt voor ontwikkeling/testing.
// Belangrijke seed-objecten en waar ze gebruikt worden:
//  - Admin-account: e-mail `admin@biblio.local` (standaard) — gebruikt door LoginWindow, AdminUsersWindow en security checks (zie App.xaml.cs, AdminUsersWindow.xaml.cs, LoginWindow.xaml.cs)
//  - Geblokkeerd testaccount (optioneel) — handig om geblokkeerde login‑flows te testen (gebruikt in LoginWindow en AdminUsersWindow)
//  - Medewerker‑account (optioneel) — staff‑rol voor testen (AdminUsersWindow, SecurityViewModel)
//  - Voorbeeldleden en -boeken: gebruikt door UitleningWindow, BoekWindow, LidWindow (zie UitleningWindow.xaml.cs, BoekWindow.xaml.cs, LidWindow.xaml.cs)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Biblio_Models.Seed
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<BiblioDbContext>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
            var opts = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;

            //1️⃣ Database aanmaken (indien niet bestaat)
            await db.Database.MigrateAsync(); // (3) //CRUD: database migration (DDL)

            //2️⃣ Rollen aanmaken (enkel Admin en Medewerker)
            string[] rollen = { "Admin", "Medewerker" };
            foreach (var role in rollen)
            {
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
            }

            //3️⃣ Admin-gebruiker aanmaken of wachtwoord forceren (zonder tokenproviders)
            // De standaardwaarden hieronder zijn development/test defaults ("dummy" credentials).
            // Production: overschrijf via configuratie (SeedOptions in appsettings.json/user secrets) of verwijder deze seed.
            var adminEmail = string.IsNullOrWhiteSpace(opts.AdminEmail) ? "admin@biblio.local" : opts.AdminEmail;
            var desiredPwd = string.IsNullOrWhiteSpace(opts.AdminPassword) ? "Admin1234?" : opts.AdminPassword;
            var admin = await userMgr.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = string.IsNullOrWhiteSpace(opts.AdminFullName) ? "Beheerder" : opts.AdminFullName,
                    EmailConfirmed = true
                };

                var create = await userMgr.CreateAsync(admin, desiredPwd);
                if (!create.Succeeded)
                    throw new Exception("Fout bij aanmaken admin: " + string.Join(", ", create.Errors.Select(e => e.Description))); // (2) //lambda expression used in Select
            }
            else if (!string.IsNullOrWhiteSpace(opts.AdminPassword))
            {
                // Forceer wachtwoord indien admin bestaat en een admin-wachtwoord is opgegeven via config
                admin.PasswordHash = hasher.HashPassword(admin, desiredPwd);
                var upd = await userMgr.UpdateAsync(admin);
                if (!upd.Succeeded)
                    throw new Exception("Fout bij resetten admin-wachtwoord: " + string.Join(", ", upd.Errors.Select(e => e.Description))); // (2) //lambda expression
            }

            // Admin in Admin-rol zetten
            // (De Admin-rol wordt gebruikt door AdminUsersWindow, SecurityViewModel en autorisatie checks.)
            admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin != null && !await userMgr.IsInRoleAsync(admin, "Admin"))
            {
                await userMgr.AddToRoleAsync(admin, "Admin");
            }

            //4️⃣ Basisdata seeden
            if (!await db.Categorien.AnyAsync()) // (1) //LINQ AnyAsync
            {
                db.Categorien.AddRange(
                    new Categorie { Naam = "Roman" },
                    new Categorie { Naam = "Jeugd" },
                    new Categorie { Naam = "Thriller" },
                    new Categorie { Naam = "Wetenschap" }
                );
                await db.SaveChangesAsync(); // (3) //CRUD
            }

            if (!await db.Boeken.AnyAsync()) // (1) //LINQ
            {
                //dummy objecten ophalen 
                var roman = await db.Categorien.FirstAsync(c => c.Naam == "Roman"); // (1) //LINQ + (2) //lambda predicate
                var jeugd = await db.Categorien.FirstAsync(c => c.Naam == "Jeugd"); // (1) //LINQ + (2) //lambda
                var thriller = await db.Categorien.FirstAsync(c => c.Naam == "Thriller");
                var wetenschap = await db.Categorien.FirstAsync(c => c.Naam == "Wetenschap");

                // Sample books (development/test data). Deze items worden gebruikt door BoekWindow en UitleningWindow.
                db.Boeken.AddRange(
                    new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                    new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                    new Boek { Titel = "Pride and Prejudice", Auteur = "Jane Austen", Isbn = "9781503290563", CategorieID = roman.Id },
                    new Boek { Titel = "To Kill a Mockingbird", Auteur = "Harper Lee", Isbn = "9780061120084", CategorieID = roman.Id },
                    new Boek { Titel = "Brave New World", Auteur = "Aldous Huxley", Isbn = "9780060850524", CategorieID = roman.Id },

                    new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id },
                    new Boek { Titel = "Harry Potter en de Steen der Wijzen", Auteur = "J.K. Rowling", Isbn = "9781408855652", CategorieID = jeugd.Id },

                    new Boek { Titel = "The Girl with the Dragon Tattoo", Auteur = "Stieg Larsson", Isbn = "9780307454546", CategorieID = thriller.Id },
                    new Boek { Titel = "The Da Vinci Code", Auteur = "Dan Brown", Isbn = "9780307474278", CategorieID = thriller.Id },

                    new Boek { Titel = "A Brief History of Time", Auteur = "Stephen Hawking", Isbn = "9780553380163", CategorieID = wetenschap.Id },
                    new Boek { Titel = "The Selfish Gene", Auteur = "Richard Dawkins", Isbn = "9780192860927", CategorieID = wetenschap.Id }
                );
                await db.SaveChangesAsync(); // (3) //CRUD
            }

            if (!await db.Leden.AnyAsync()) // (1) //LINQ
            {
                // Sample members (development/test data). Used in UitleningWindow and LidWindow.
                db.Leden.AddRange(

                    new Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com" },
                    new Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com" }
                );
                await db.SaveChangesAsync(); // (3) //CRUD
            }

            // 4️⃣ Seed languages (Talen) if not present
            if (!await db.Set<Taal>().AnyAsync())
            {
                db.Set<Taal>().AddRange(
                    new Taal { Code = "nl", Naam = "Nederlands", IsDefault = true },
                    new Taal { Code = "en", Naam = "English", IsDefault = false }
                );
                await db.SaveChangesAsync();
            }

            //5️⃣ Optioneel: seeding van test accounts wanneer dit expliciet is ingeschakeld via SeedOptions
            if (opts.CreateTestAccounts)
            {
                // Blocked account
                var blockedEmail = string.IsNullOrWhiteSpace(opts.BlockedEmail) ? "blocked@biblio.local" : opts.BlockedEmail;
                var blockedPwd = string.IsNullOrWhiteSpace(opts.BlockedPassword) ? "Test!23456" : opts.BlockedPassword;
                var blocked = await userMgr.FindByEmailAsync(blockedEmail);
                if (blocked == null)
                {
                    blocked = new AppUser
                    {

                        UserName = blockedEmail,
                        Email = blockedEmail,
                        FullName = "Geblokkeerde Gebruiker",
                        EmailConfirmed = true,
                        IsBlocked = true
                    };
                    var createBlocked = await userMgr.CreateAsync(blocked, blockedPwd);
                    if (createBlocked.Succeeded)
                    {
                        if (await roleMgr.RoleExistsAsync("Medewerker"))
                            await userMgr.AddToRoleAsync(blocked, "Medewerker");
                    }
                }

                // Staff account
                var staffEmail = string.IsNullOrWhiteSpace(opts.StaffEmail) ? "medewerker@biblio.local" : opts.StaffEmail;
                var staffPwd = string.IsNullOrWhiteSpace(opts.StaffPassword) ? "test1234?" : opts.StaffPassword;
                var staff = await userMgr.FindByEmailAsync(staffEmail);
                if (staff == null)
                {
                    staff = new AppUser
                    {
                        UserName = staffEmail,
                        Email = staffEmail,
                        FullName = "Standaard Medewerker",
                        EmailConfirmed = true
                    };

                    var createStaff = await userMgr.CreateAsync(staff, staffPwd);
                    if (createStaff.Succeeded)
                    {
                        if (await roleMgr.RoleExistsAsync("Medewerker"))
                            await userMgr.AddToRoleAsync(staff, "Medewerker");
                    }
                }
            }
        }

        /// <summary>
        /// Seed methode voor MAUI (LocalDbContext - SQLite offline database).
        /// Deze methode wordt aangeroepen vanuit MauiProgram.cs tijdens app initialisatie.
        /// Seeded alleen basisdata (geen Identity users/roles - MAUI heeft geen Identity).
        /// </summary>
        public static async Task SeedAsync(LocalDbContext db, SeedOptions? options = null)
        {
            options ??= new SeedOptions { NumberOfBooks = 20, NumberOfMembers = 10 };

            // 1️⃣ Seed categorieën
            if (!await db.Categorien.AnyAsync())
            {
                db.Categorien.AddRange(
                    new Categorie { Naam = "Roman" },
                    new Categorie { Naam = "Jeugd" },
                    new Categorie { Naam = "Thriller" },
                    new Categorie { Naam = "Wetenschap" },
                    new Categorie { Naam = "Biografie" }
                );
                await db.SaveChangesAsync();
            }

            // 2️⃣ Seed boeken (20 items voor development/testing)
            if (!await db.Boeken.AnyAsync())
            {
                var categories = await db.Categorien.ToListAsync();
                var roman = categories.First(c => c.Naam == "Roman");
                var jeugd = categories.First(c => c.Naam == "Jeugd");
                var thriller = categories.First(c => c.Naam == "Thriller");
                var wetenschap = categories.First(c => c.Naam == "Wetenschap");
                var biografie = categories.First(c => c.Naam == "Biografie");

                db.Boeken.AddRange(
                    // Romans (8 items)
                    new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                    new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                    new Boek { Titel = "Pride and Prejudice", Auteur = "Jane Austen", Isbn = "9781503290563", CategorieID = roman.Id },
                    new Boek { Titel = "To Kill a Mockingbird", Auteur = "Harper Lee", Isbn = "9780061120084", CategorieID = roman.Id },
                    new Boek { Titel = "Brave New World", Auteur = "Aldous Huxley", Isbn = "9780060850524", CategorieID = roman.Id },
                    new Boek { Titel = "The Great Gatsby", Auteur = "F. Scott Fitzgerald", Isbn = "9780743273565", CategorieID = roman.Id },
                    new Boek { Titel = "The Catcher in the Rye", Auteur = "J.D. Salinger", Isbn = "9780316769488", CategorieID = roman.Id },
                    new Boek { Titel = "Animal Farm", Auteur = "George Orwell", Isbn = "9780451526342", CategorieID = roman.Id },

                    // Jeugd (4 items)
                    new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id },
                    new Boek { Titel = "Harry Potter en de Steen der Wijzen", Auteur = "J.K. Rowling", Isbn = "9781408855652", CategorieID = jeugd.Id },
                    new Boek { Titel = "Charlie and the Chocolate Factory", Auteur = "Roald Dahl", Isbn = "9780142410318", CategorieID = jeugd.Id },
                    new Boek { Titel = "The Lion, the Witch and the Wardrobe", Auteur = "C.S. Lewis", Isbn = "9780064471046", CategorieID = jeugd.Id },

                    // Thrillers (4 items)
                    new Boek { Titel = "The Girl with the Dragon Tattoo", Auteur = "Stieg Larsson", Isbn = "9780307454546", CategorieID = thriller.Id },
                    new Boek { Titel = "The Da Vinci Code", Auteur = "Dan Brown", Isbn = "9780307474278", CategorieID = thriller.Id },
                    new Boek { Titel = "Gone Girl", Auteur = "Gillian Flynn", Isbn = "9780307588371", CategorieID = thriller.Id },
                    new Boek { Titel = "The Silence of the Lambs", Auteur = "Thomas Harris", Isbn = "9780312924584", CategorieID = thriller.Id },

                    // Wetenschap (2 items)
                    new Boek { Titel = "A Brief History of Time", Auteur = "Stephen Hawking", Isbn = "9780553380163", CategorieID = wetenschap.Id },
                    new Boek { Titel = "The Selfish Gene", Auteur = "Richard Dawkins", Isbn = "9780192860927", CategorieID = wetenschap.Id },

                    // Biografie (2 items)
                    new Boek { Titel = "Steve Jobs", Auteur = "Walter Isaacson", Isbn = "9781451648539", CategorieID = biografie.Id },
                    new Boek { Titel = "The Diary of a Young Girl", Auteur = "Anne Frank", Isbn = "9780553296983", CategorieID = biografie.Id }
                );
                await db.SaveChangesAsync();
            }

            // 3️⃣ Seed leden (10 items voor development/testing)
            if (!await db.Leden.AnyAsync())
            {
                db.Leden.AddRange(
                    new Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com", Telefoon = "0471234567" },
                    new Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com", Telefoon = "0472345678" },
                    new Lid { Voornaam = "Peter", AchterNaam = "Janssen", Email = "peter.janssen@example.com", Telefoon = "0473456789" },
                    new Lid { Voornaam = "Marie", AchterNaam = "Dubois", Email = "marie.dubois@example.com", Telefoon = "0474567890" },
                    new Lid { Voornaam = "Luc", AchterNaam = "Vermeulen", Email = "luc.vermeulen@example.com", Telefoon = "0475678901" },
                    new Lid { Voornaam = "Sophie", AchterNaam = "Maes", Email = "sophie.maes@example.com", Telefoon = "0476789012" },
                    new Lid { Voornaam = "Tom", AchterNaam = "Willems", Email = "tom.willems@example.com", Telefoon = "0477890123" },
                    new Lid { Voornaam = "Emma", AchterNaam = "Claes", Email = "emma.claes@example.com", Telefoon = "0478901234" },
                    new Lid { Voornaam = "Lucas", AchterNaam = "Goossens", Email = "lucas.goossens@example.com", Telefoon = "0479012345" },
                    new Lid { Voornaam = "Nina", AchterNaam = "Wouters", Email = "nina.wouters@example.com", Telefoon = "0470123456" }
                );
                await db.SaveChangesAsync();
            }

            // 4️⃣ Seed talen (voor meertalige UI)
            if (!await db.Set<Taal>().AnyAsync())
            {
                db.Set<Taal>().AddRange(
                    new Taal { Code = "nl", Naam = "Nederlands", IsDefault = true },
                    new Taal { Code = "en", Naam = "English", IsDefault = false },
                    new Taal { Code = "fr", Naam = "Français", IsDefault = false }
                );
                await db.SaveChangesAsync();
            }

            // 5️⃣ Seed uitleningen (15 sample items voor testing)
            if (!await db.Leningens.AnyAsync())
            {
                var boeken = await db.Boeken.Take(15).ToListAsync();
                var leden = await db.Leden.Take(10).ToListAsync();

                if (boeken.Any() && leden.Any())
                {
                    var uitleningen = new List<Lenen>();
                    var random = new Random(42); // Fixed seed voor consistente test data

                    for (int i = 0; i < Math.Min(15, boeken.Count); i++)
                    {
                        var lid = leden[i % leden.Count];
                        var boek = boeken[i];
                        var startDate = DateTime.Now.AddDays(-random.Next(1, 30));
                        var dueDate = startDate.AddDays(14);

                        // 70% van uitleningen zijn nog open (niet ingeleverd)
                        DateTime? returnedAt = (i % 10 < 7) ? null : startDate.AddDays(random.Next(7, 14));

                        uitleningen.Add(new Lenen
                        {
                            BoekId = boek.Id,
                            LidId = lid.Id,
                            StartDate = startDate,
                            DueDate = dueDate,
                            ReturnedAt = returnedAt
                        });
                    }

                    db.Leningens.AddRange(uitleningen);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
