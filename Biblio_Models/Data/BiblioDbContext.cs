using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Biblio_Models.Data
{
    public class BiblioDbContext : IdentityDbContext<AppUser>
    {
        public BiblioDbContext(DbContextOptions<BiblioDbContext> options) : base(options) { }

        public DbSet<Boek> Boeken { get; set; } = null!;
        public DbSet<Lid> Leden { get; set; } = null!;
        public DbSet<Lenen> Leningens { get; set; } = null!;
        public DbSet<Categorie> Categorien { get; set; } = null!;
        public DbSet<Taal> Talen { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string? connectionString = null;

                connectionString = Environment.GetEnvironmentVariable("BIBLIO_CONNECTION");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from BIBLIO_CONNECTION"); } catch { }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from ConnectionStrings__DefaultConnection (env)"); } catch { }
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable("PublicConnection");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from PublicConnection (env)"); } catch { }
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable("PublicConnection_Azure");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from PublicConnection_Azure (env)"); } catch { }
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from AZURE_SQL_CONNECTIONSTRING (env)"); } catch { }
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";
                    try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Falling back to LocalDB connection"); } catch { }
                }

                optionsBuilder.UseSqlServer(connectionString);
            }

            base.OnConfiguring(optionsBuilder);
        }


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Boek>(e =>
            {
                e.ToTable("Boeken");
                e.Property(x => x.Id).HasColumnName("BoekId");
                e.Property(x => x.Titel).HasColumnName("Titel");
                e.Property(x => x.Auteur).HasColumnName("Auteur");
                e.Property(x => x.Isbn).HasColumnName("ISBN");
            });

            b.Entity<Lid>(e =>
            {
                e.ToTable("Leden");
                e.Property(x => x.Id).HasColumnName("LidId");
                e.Property(x => x.Voornaam).HasColumnName("Voornaam");
                e.Property(x => x.AchterNaam).HasColumnName("Naam");
                e.Property(x => x.Telefoon).HasColumnName("Tel");
            });

            b.Entity<Lenen>(e =>
            {
                e.ToTable("Uitleningen");
                e.Property(x => x.Id).HasColumnName("UitleningId");
                e.Property(x => x.BoekId).HasColumnName("BoekId");
                e.Property(x => x.LidId).HasColumnName("LidId");
                e.Property(x => x.StartDate).HasColumnName("StartDatum");
                e.Property(x => x.DueDate).HasColumnName("EindDatum");
                e.Property(x => x.ReturnedAt).HasColumnName("IngeleverdOp");

                e.HasIndex(x => x.BoekId)
                .HasFilter("([IngeleverdOp] IS NULL)")
                .HasDatabaseName("IX_Uitleningen_BoekId_Actief")
                .IsUnique();
            });

            b.Entity<Boek>()
            .HasOne(x => x.categorie)
            .WithMany(c => c.Boeken)
            .HasForeignKey(x => x.CategorieID)
            .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Lenen>()
            .HasOne(l => l.Boek)
            .WithMany(bk => bk.leent)
            .HasForeignKey(l => l.BoekId);

            b.Entity<Lenen>()
            .HasOne(l => l.Lid)
            .WithMany(m => m.Leningens)
            .HasForeignKey(l => l.LidId);

            b.Entity<Boek>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Lid>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Categorie>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Lenen>().HasQueryFilter(e => !e.IsDeleted);

            b.Entity<Taal>(e =>
            {
                e.ToTable("Talen");
                e.Property(t => t.Id).HasColumnName("Id");
                e.Property(t => t.Naam).HasColumnName("Naam").HasMaxLength(120).IsRequired();
                e.Property(t => t.Code).HasColumnName("Code").HasMaxLength(10).IsRequired();
                e.Property(t => t.IsDefault).HasColumnName("IsDefault");
                e.Property(t => t.IsDeleted).HasColumnName("IsDeleted");
            });

            b.Entity<Taal>().HasQueryFilter(t => !t.IsDeleted);

            b.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshTokens");
                e.Property(r => r.Id).HasColumnName("Id");
                e.Property(r => r.Token).HasColumnName("Token").IsRequired();
                e.Property(r => r.UserId).HasColumnName("UserId").IsRequired();
                e.Property(r => r.Expires).HasColumnName("Expires");
                e.Property(r => r.CreatedUtc).HasColumnName("CreatedUtc");
                e.Property(r => r.Revoked).HasColumnName("Revoked");
                e.Property(r => r.ReplacedByToken).HasColumnName("ReplacedByToken");
            });

            b.Entity<Lid>()
            .HasIndex(m => m.Email)
            .IsUnique()
            .HasFilter("([Email] IS NOT NULL AND [Email] <> '')");

            b.Entity<Boek>()
            .HasIndex(bk => bk.Isbn)
            .IsUnique()
            .HasFilter("([Isbn] IS NOT NULL AND [Isbn] <> '')");
        }

        // Zet de SeedAsync-methode HIER, buiten de andere methodes maar binnen de class!
        public static async Task SeedAsync(BiblioDbContext ctx)
        {
            try
            {
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

                if (!ctx.Boeken.Any())
                {
                    var roman = ctx.Categorien.FirstOrDefault(c => c.Naam == "Roman") ?? ctx.Categorien.First();
                    var jeugd = ctx.Categorien.FirstOrDefault(c => c.Naam == "Jeugd") ?? ctx.Categorien.First();
                    var thriller = ctx.Categorien.FirstOrDefault(c => c.Naam == "Thriller") ?? ctx.Categorien.First();
                    var wetenschap = ctx.Categorien.FirstOrDefault(c => c.Naam == "Wetenschap") ?? ctx.Categorien.First();
                    ctx.Boeken.AddRange(
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
                    await ctx.SaveChangesAsync();
                }

                if (!ctx.Leden.Any())
                {
                    ctx.Leden.AddRange(
                        new Lid { Voornaam = "Jan", AchterNaam = "Peeters", Email = "jan.peeters@example.com", Telefoon = "+32 470 11 22 33" },
                        new Lid { Voornaam = "Sara", AchterNaam = "De Smet", Email = "sara.desmet@example.com", Telefoon = "+32 480 44 55 66" }
                    );
                    await ctx.SaveChangesAsync();
                }

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