using Microsoft.EntityFrameworkCore;
using Biblio_Models.Entiteiten;
using System;

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
        public DbSet<LocalLenen> LocalLeningens { get; set; } = null!;
        public DbSet<Categorie> Categorien { get; set; } = null!;
        public DbSet<Taal> Talen { get; set; } = null!;
        public DbSet<LocalLid> LocalLeden { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                var folder = Environment.SpecialFolder.LocalApplicationData;
                var path = Environment.GetFolderPath(folder);
                var DbPath = System.IO.Path.Join(path, "BiblioApp.db");
                options.UseSqlite($"Data Source={DbPath}");
            }
        }
    }
}