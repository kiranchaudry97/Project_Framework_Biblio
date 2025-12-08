using System.Threading.Tasks;
using Biblio_App.Models;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Biblio_App.Services
{
    public class EfGegevensProvider : IGegevensProvider
    {
        private readonly IDbContextFactory<BiblioDbContext> _dbFactory;

        public EfGegevensProvider(IDbContextFactory<BiblioDbContext> dbFactory)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        public async Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            var boeken = await db.Boeken.CountAsync();
            var leden = await db.Leden.CountAsync();
            var openUitleningen = await db.Leningens.CountAsync(l => l.ReturnedAt == null);
            return (boeken, leden, openUitleningen);
        }
    }
}
