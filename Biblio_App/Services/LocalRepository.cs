using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Microsoft.EntityFrameworkCore;

namespace Biblio_App.Services
{
    public class LocalRepository : ILocalRepository
    {
        private readonly IDbContextFactory<LocalDbContext> _dbFactory;

        public LocalRepository(IDbContextFactory<LocalDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // Boeken
        public async Task<List<Boek>> GetBoekenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Boeken.AsNoTracking().Include(b => b.categorie).Where(b => b.IsDeleted == false).ToListAsync();
        }

        public async Task SaveBoekAsync(Boek boek)
        {
            using var db = _dbFactory.CreateDbContext();
            if (boek.Id == 0) db.Boeken.Add(boek); else db.Boeken.Update(boek);
            await db.SaveChangesAsync();
        }

        public async Task SaveBoekenAsync(IEnumerable<Boek> boeken)
        {
            using var db = _dbFactory.CreateDbContext();
            foreach (var b in boeken)
            {
                var existing = await db.Boeken.FindAsync(b.Id);
                if (existing == null) db.Boeken.Add(b); else db.Entry(existing).CurrentValues.SetValues(b);
            }
            await db.SaveChangesAsync();
        }

        public async Task DeleteBoekAsync(int id)
        {
            using var db = _dbFactory.CreateDbContext();
            var existing = await db.Boeken.FindAsync(id);
            if (existing != null) { db.Boeken.Remove(existing); await db.SaveChangesAsync(); }
        }

        // Leden
        public async Task<List<Lid>> GetLedenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Leden.AsNoTracking().ToListAsync();
        }

        public async Task SaveLidAsync(Lid lid)
        {
            using var db = _dbFactory.CreateDbContext();
            if (lid.Id == 0) db.Leden.Add(lid); else db.Leden.Update(lid);
            await db.SaveChangesAsync();
        }

        public async Task DeleteLidAsync(int id)
        {
            using var db = _dbFactory.CreateDbContext();
            var existing = await db.Leden.FindAsync(id);
            if (existing != null) { db.Leden.Remove(existing); await db.SaveChangesAsync(); }
        }

        // Uitleningen
        public async Task<List<Lenen>> GetUitleningenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Leningens.AsNoTracking().Include(l => l.Boek).Include(l => l.Lid).ToListAsync();
        }

        public async Task SaveUitleningAsync(Lenen uitlening)
        {
            using var db = _dbFactory.CreateDbContext();
            if (uitlening.Id == 0) db.Leningens.Add(uitlening); else db.Leningens.Update(uitlening);
            await db.SaveChangesAsync();
        }

        public async Task DeleteUitleningAsync(int id)
        {
            using var db = _dbFactory.CreateDbContext();
            var existing = await db.Leningens.FindAsync(id);
            if (existing != null) { db.Leningens.Remove(existing); await db.SaveChangesAsync(); }
        }

        // Categorien
        public async Task<List<Categorie>> GetCategorieenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Categorien.AsNoTracking().Where(c => c.IsDeleted == false).OrderBy(c => c.Naam).ToListAsync();
        }

        public async Task SaveCategorieAsync(Categorie categorie)
        {
            using var db = _dbFactory.CreateDbContext();
            if (categorie.Id == 0) db.Categorien.Add(categorie); else db.Categorien.Update(categorie);
            await db.SaveChangesAsync();
        }

        public async Task SaveCategorieenAsync(IEnumerable<Categorie> categorien)
        {
            using var db = _dbFactory.CreateDbContext();
            foreach (var c in categorien)
            {
                var existing = await db.Categorien.FindAsync(c.Id);
                if (existing == null) db.Categorien.Add(c); else db.Entry(existing).CurrentValues.SetValues(c);
            }
            await db.SaveChangesAsync();
        }

        public async Task DeleteCategorieAsync(int id)
        {
            using var db = _dbFactory.CreateDbContext();
            var existing = await db.Categorien.FindAsync(id);
            if (existing != null) { db.Categorien.Remove(existing); await db.SaveChangesAsync(); }
        }
    }
}