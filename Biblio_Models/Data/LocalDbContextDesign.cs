using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Biblio_Models.Data
{
    public class LocalDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<LocalDbContext>();
            // Design-time only: use a local sqlite file for scaffold/migration generation
            builder.UseSqlite("Data Source=local_migrations.db");
            return new LocalDbContext(builder.Options);
        }
    }
}
