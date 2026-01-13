using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Biblio_Models.Data
{
    public class LocalDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<LocalDbContext>();
            // Design-time only: use the same local SQLite file as the runtime (LocalApplicationData/BiblioApp.db)
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = System.IO.Path.Join(path, "BiblioApp.db");
            builder.UseSqlite($"Data Source={dbPath}");
            return new LocalDbContext(builder.Options);
        }
    }
}
