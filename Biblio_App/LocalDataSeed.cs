using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;

namespace Biblio_App
{
    internal static class LocalDataSeed
    {
        public static async Task SeedLocalDataAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetService<IDbContextFactory<LocalDbContext>>();
            if (dbFactory == null) return;

            using var db = dbFactory.CreateDbContext();

            // Categories
            if (!db.Categorien.Any())
            {
                db.Categorien.AddRange(
                    new Categorie { Naam = "Roman" },
                    new Categorie { Naam = "Jeugd" },
                    new Categorie { Naam = "Thriller" },
                    new Categorie { Naam = "Wetenschap" }
                );
                await db.SaveChangesAsync();
            }

            // Books
            if (!db.Boeken.Any())
            {
                var roman = db.Categorien.FirstOrDefault(c => c.Naam == "Roman");
                var jeugd = db.Categorien.FirstOrDefault(c => c.Naam == "Jeugd");
                var thriller = db.Categorien.FirstOrDefault(c => c.Naam == "Thriller");
                var wetenschap = db.Categorien.FirstOrDefault(c => c.Naam == "Wetenschap");

                db.Boeken.AddRange(
                    new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman?.Id ?? 0 },
                    new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman?.Id ?? 0 },
                    new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd?.Id ?? 0 },
                    new Boek { Titel = "The Girl with the Dragon Tattoo", Auteur = "Stieg Larsson", Isbn = "9780307454546", CategorieID = thriller?.Id ?? 0 },
                    new Boek { Titel = "A Brief History of Time", Auteur = "Stephen Hawking", Isbn = "9780553380163", CategorieID = wetenschap?.Id ?? 0 }
                );
                await db.SaveChangesAsync();
            }

            // Members
            if (!db.Leden.Any())
            {
                db.Leden.AddRange(
                    new Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com" },
                    new Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com" }
                );
                await db.SaveChangesAsync();
            }

            // Loans (Uitleningen)
            if (!db.Leningens.Any())
            {
                var firstBook = db.Boeken.AsNoTracking().FirstOrDefault();
                var firstMember = db.Leden.AsNoTracking().FirstOrDefault();
                if (firstBook != null && firstMember != null)
                {
                    db.Leningens.AddRange(
                        new Lenen
                        {
                            BoekId = firstBook.Id,
                            LidId = firstMember.Id,
                            StartDate = DateTime.Now.AddDays(-7),
                            DueDate = DateTime.Now.AddDays(7),
                            ReturnedAt = null,
                            IsClosed = false
                        }
                    );
                    await db.SaveChangesAsync();
                }
            }

            // Languages (Talen)
            if (!db.Set<Taal>().Any())
            {
                db.Set<Taal>().AddRange(
                    new Taal { Code = "nl", Naam = "Nederlands", IsDefault = true },
                    new Taal { Code = "en", Naam = "English", IsDefault = false },
                    new Taal { Code = "fr", Naam = "Français", IsDefault = false }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
