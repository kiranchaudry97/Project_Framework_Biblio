using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Biblio_Models.Data
{
    public class LocalDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<LocalDbContext>();
            // Design-time only: use a predictable path inside the repository so migrations
            // can be created consistently on the developer machine. This file is not used
            // at runtime on devices; runtime path is configured in the MAUI app.
            var repoRoot = System.IO.Directory.GetCurrentDirectory();
            var dbPath = System.IO.Path.Join(repoRoot, "bibliodatabase.db");
            builder.UseSqlite($"Data Source={dbPath}");
            return new LocalDbContext(builder.Options);
        }
    }
}
