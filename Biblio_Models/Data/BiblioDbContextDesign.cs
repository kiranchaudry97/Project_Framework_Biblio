using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Biblio_Models.Data
{
    public class BiblioDbContextDesign : IDesignTimeDbContextFactory<BiblioDbContext>
    {
        public BiblioDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BiblioDbContext>();
            var connectionString = Environment.GetEnvironmentVariable("BIBLIO_CONNECTION")
                ?? "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            builder.UseSqlServer(connectionString);
            return new BiblioDbContext(builder.Options);
        }
    }
}
