using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Biblio_Models.Data
{
    // Design-time factory that forces SQLite so migrations for mobile/local use a SQLite provider
    public class BiblioDbContextDesignTimeSqliteFactory : IDesignTimeDbContextFactory<BiblioDbContext>
    {
        public BiblioDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BiblioDbContext>();
            // Use a local sqlite file for design-time migrations generation
            builder.UseSqlite("Data Source=mobile_migrations.db");
            return new BiblioDbContext(builder.Options);
        }
    }
}
