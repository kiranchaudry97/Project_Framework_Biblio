using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
// zie commict bericht voor meer informatie over AppUser

namespace Biblio_Models.Data
{
    public class BiblioDbContext : IdentityDbContext<AppUser>
    {
        public BiblioDbContext(DbContextOptions<BiblioDbContext> options) : base(options) { }

        public DbSet<Boek> Boeken { get; set; } = null!;
        public DbSet<Lid> Leden { get; set; } = null!;
        public DbSet<Lenen> Leningens { get; set; } = null!;
        public DbSet<Categorie> Categorien { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Environment.GetEnvironmentVariable("BIBLIO_CONNECTION")
                    ?? "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

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
                e.Property(x => x.Adres).HasColumnName("Adres");
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

                // Max. één actieve uitlening per boek (IngeleverdOp is NULL)
                e.HasIndex(x => x.BoekId)
                .HasFilter("([IngeleverdOp] IS NULL)")
                .HasDatabaseName("IX_Uitleningen_BoekId_Actief")
                .IsUnique();
            });

            // Relaties
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


            // Soft‑delete global filters
            b.Entity<Boek>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Lid>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Categorie>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Lenen>().HasQueryFilter(e => !e.IsDeleted);

            // Unieke indexes voor data-integriteit
            b.Entity<Lid>()
            .HasIndex(m => m.Email)
            .IsUnique()
            .HasFilter("([Email] IS NOT NULL AND [Email] <> '')");

            b.Entity<Boek>()
            .HasIndex(bk => bk.Isbn)
            .IsUnique()
            .HasFilter("([Isbn] IS NOT NULL AND [Isbn] <> '')");
        }
    }

    internal class BiblioDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BiblioDbContext>
    {
        public BiblioDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BiblioDbContext>();
            var connectionString = Environment.GetEnvironmentVariable("BIBLIO_CONNECTION")
                ?? "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);

            return new BiblioDbContext(optionsBuilder.Options);
        }
    }
}