// SeedData contains EF Core queries (LINQ) and CRUD operations and uses lambda expressions.

// 1) //LINQ - queries such as AnyAsync, FirstAsync, Where are used in this file
// 2) //lambda expression - used in Select, FirstAsync predicates and other delegates
// 3) //CRUD - AddRange, SaveChangesAsync, Database.MigrateAsync

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
                admin.PasswordHash = hasher.HashPassword(admin, desiredPwd);
                var upd = await userMgr.UpdateAsync(admin);
                if (!upd.Succeeded)
                    throw new Exception("Fout bij resetten admin-wachtwoord: " + string.Join(", ", upd.Errors.Select(e => e.Description))); // (2) //lambda expression
            }

            // Admin in Admin-rol zetten
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
                var roman = await db.Categorien.FirstAsync(c => c.Naam == "Roman"); // (1) //LINQ + (2) //lambda predicate
                var jeugd = await db.Categorien.FirstAsync(c => c.Naam == "Jeugd"); // (1) //LINQ + (2) //lambda
                var thriller = await db.Categorien.FirstAsync(c => c.Naam == "Thriller");
                var wetenschap = await db.Categorien.FirstAsync(c => c.Naam == "Wetenschap");

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
                db.Leden.AddRange(
                    new Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com" },
                    new Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com" }
                );
                await db.SaveChangesAsync(); // (3) //CRUD
            }

            //5️⃣ Optioneel: seeding van een geblokkeerd test-account
            var blockedEmail = "blocked@biblio.local";
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
                var createBlocked = await userMgr.CreateAsync(blocked, "Test!23456");
                if (createBlocked.Succeeded)
                {
                    if (await roleMgr.RoleExistsAsync("Medewerker"))
                        await userMgr.AddToRoleAsync(blocked, "Medewerker");
                }
            }

            //6️⃣ Optioneel: seeding van een standaard medewerker account
            var staffEmail = "medewerker@biblio.local";
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

                var createStaff = await userMgr.CreateAsync(staff, "test1234?");
                if (createStaff.Succeeded)
                {
                    if (await roleMgr.RoleExistsAsync("Medewerker"))
                        await userMgr.AddToRoleAsync(staff, "Medewerker");
                }
            }
        }
    }
}
