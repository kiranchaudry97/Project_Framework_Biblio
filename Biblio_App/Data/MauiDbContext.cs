using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Data
{
    // MAUI-specific DbContext for the mobile app. This mirrors the local schema used by the
    // shared model but targets SQLite and contains a simple Seed method used by the client.
    public class MauiDbContext : DbContext
    {
        public MauiDbContext(DbContextOptions<MauiDbContext> options) : base(options)
        {
        }

        public DbSet<Boek> Boeken { get; set; } = null!;
        public DbSet<Lid> Leden { get; set; } = null!;
        public DbSet<Lenen> Leningens { get; set; } = null!;
        public DbSet<Categorie> Categorien { get; set; } = null!;
        public DbSet<Taal> Talen { get; set; } = null!;

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

            b.Entity<Categorie>(e =>
            {
                e.ToTable("Categorien");
            });

            b.Entity<Taal>(e =>
            {
                e.ToTable("Talen");
                e.Property(t => t.Id).HasColumnName("Id");
                e.Property(t => t.Naam).HasColumnName("Naam").HasMaxLength(120).IsRequired();
                e.Property(t => t.Code).HasColumnName("Code").HasMaxLength(10).IsRequired();
                e.Property(t => t.IsDefault).HasColumnName("IsDefault");
                e.Property(t => t.IsDeleted).HasColumnName("IsDeleted");
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
            b.Entity<Taal>().HasQueryFilter(t => !t.IsDeleted);
        }

        // Simple client-side seeding used by the MAUI app when starting with an empty DB.
        public static async Task SeedAsync(MauiDbContext ctx)
        {
            // Ensure created then seed minimal demo data if empty.
            await ctx.Database.EnsureCreatedAsync();

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
                var roman = ctx.Categorien.First(c => c.Naam == "Roman");
                var jeugd = ctx.Categorien.First(c => c.Naam == "Jeugd");
                var thriller = ctx.Categorien.First(c => c.Naam == "Thriller");
                var wetenschap = ctx.Categorien.First(c => c.Naam == "Wetenschap");

                ctx.Boeken.AddRange(
                    new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                    new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                    new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id }
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
                var firstMember = ctx.Leden.FirstOrDefault();
                if (firstBook != null && firstMember != null)
                {
                    ctx.Leningens.Add(new Lenen
                    {
                        BoekId = firstBook.Id,
                        LidId = firstMember.Id,
                        StartDate = DateTime.Today.AddDays(-7),
                        DueDate = DateTime.Today.AddDays(7),
                        ReturnedAt = null
                    });
                    await ctx.SaveChangesAsync();
                }
            }
        }
    }
}
