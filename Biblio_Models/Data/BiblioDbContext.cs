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
        public DbSet<Taal> Talen { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Try several environment/config keys so both user-secrets, env vars and CI pipelines are supported
                string? connectionString = null;

                // 1. Explicit environment variable used by some deployments/scripts
                connectionString = Environment.GetEnvironmentVariable("BIBLIO_CONNECTION");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from BIBLIO_CONNECTION"); } catch { }
                }

                // 2. Typical ASP.NET Core environment variable format for ConnectionStrings:DefaultConnection
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from ConnectionStrings__DefaultConnection (env)"); } catch { }
                    }
                }

                // 3. Legacy/public keys used by this solution
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

                // 4. Azure style named connection (used for managed identity scenarios)
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        try { System.Diagnostics.Debug.WriteLine($"[BiblioDbContext] Using connection from AZURE_SQL_CONNECTIONSTRING (env)"); } catch { }
                    }
                }

                // 5. Fallback to LocalDB (development)
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

            // Taal entity mapping
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

            // register RefreshToken entity
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
    
        /*
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
        */
    }
}