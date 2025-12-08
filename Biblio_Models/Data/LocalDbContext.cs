using Microsoft.EntityFrameworkCore;
using Biblio_Models.Entiteiten;

namespace Biblio_Models.Data
{
    // Lightweight local DbContext used by MAUI for SQLite scenarios
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
        {
        }

        public DbSet<Boek> Boeken { get; set; } = null!;
        public DbSet<Lid> Leden { get; set; } = null!;
        public DbSet<Lenen> Leningens { get; set; } = null!;
        public DbSet<Categorie> Categorien { get; set; } = null!;
        public DbSet<Taal> Talen { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Reuse mappings from BiblioDbContext conventions; no extra mapping required here.
            base.OnModelCreating(modelBuilder);
        }
    }
}
