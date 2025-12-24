using Microsoft.EntityFrameworkCore;
using Biblio_Models.Entiteiten;
using System;

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
    }
}