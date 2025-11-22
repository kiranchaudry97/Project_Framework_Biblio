using System.Threading.Tasks;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;

namespace Biblio_App.Services
{
    public class EfGegevensProvider : IGegevensProvider
    {
        private readonly BiblioDbContext _db;

        public EfGegevensProvider(BiblioDbContext db)
        {
            _db = db;
        }

        public async Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync()
        {
            var boeken = await _db.Boeken.CountAsync();
            var leden = await _db.Leden.CountAsync();
            var openUitleningen = await _db.Leningens.CountAsync(l => l.ReturnedAt == null);
            return (boeken, leden, openUitleningen);
        }
    }
}
