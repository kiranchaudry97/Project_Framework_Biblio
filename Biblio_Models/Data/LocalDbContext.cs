    using Microsoft.EntityFrameworkCore;
    using Biblio_Models.Entiteiten;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    namespace Biblio_Models.Data
    {
        public class LocalDbContext : DbContext
        {
            public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
            {
            }

            public DbSet<Boek> Boeken { get; set; } = null!;
            public DbSet<Lid> Leden { get; set; } = null!;
            public DbSet<Lenen> Leningens { get; set; } = null!;
            public DbSet<LocalLenen> LocalLeningens { get; set; } = null!;
            public DbSet<Categorie> Categorien { get; set; } = null!;
            public DbSet<Taal> Talen { get; set; } = null!;
            public DbSet<LocalLid> LocalLeden { get; set; } = null!;

    

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if no provider has been configured by DI.
        if (!optionsBuilder.IsConfigured)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = System.IO.Path.Join(path, "BiblioApp.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    public static async Task SeedAsync(LocalDbContext ctx)
    {
        try
        {
            // Seed categories
            if (!ctx.Categorien.Any())
            {
                ctx.Categorien.AddRange(
                    new Categorie { Naam = "Roman" },
                    new Categorie { Naam = "Jeugd" },
                    new Categorie { Naam = "Thriller" },
                    new Categorie { Naam = "Wetenschap" }
                );
                await ctx.SaveChangesAsync();
            }

            // Seed books
            if (!ctx.Boeken.Any())
            {
                var roman = ctx.Categorien.FirstOrDefault(c => c.Naam == "Roman") ?? ctx.Categorien.First();
                ctx.Boeken.AddRange(
                    new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                    new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id }
                );
                await ctx.SaveChangesAsync();
            }

            // Seed members
            if (!ctx.Leden.Any())
            {
                ctx.Leden.AddRange(
                    new Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com", Telefoon = "+32 470 11 22 33" },
                    new Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com", Telefoon = "+32 480 44 55 66" }
                );
                await ctx.SaveChangesAsync();
            }

            // Seed loans
            if (!ctx.Leningens.Any())
            {
                var firstBook = ctx.Boeken.FirstOrDefault();
                var secondBook = ctx.Boeken.Skip(1).FirstOrDefault();
                var firstMember = ctx.Leden.FirstOrDefault();
                var secondMember = ctx.Leden.Skip(1).FirstOrDefault();

                if (firstBook != null && firstMember != null)
                {
                    ctx.Leningens.Add(new Lenen
                    {
                        BoekId = firstBook.Id,
                        LidId = firstMember.Id,
                        StartDate = DateTime.Today.AddDays(-20),
                        DueDate = DateTime.Today.AddDays(-6),
                        ReturnedAt = null
                    });
                }

                if (secondBook != null && secondMember != null)
                {
                    ctx.Leningens.Add(new Lenen
                    {
                        BoekId = secondBook.Id,
                        LidId = secondMember.Id,
                        StartDate = DateTime.Today.AddDays(-10),
                        DueDate = DateTime.Today.AddDays(5),
                        ReturnedAt = null
                    });
                }

                await ctx.SaveChangesAsync();
            }
        }
        catch
        {
        }
    }
    }
}