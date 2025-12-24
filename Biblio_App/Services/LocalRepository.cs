using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Microsoft.EntityFrameworkCore;
using Biblio_App.Infrastructure;
using System;

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
            try
            {
                using var db = _dbFactory.CreateDbContext();

                // If new and ISBN provided, try to find existing by ISBN to avoid duplicates
                if (boek.Id == 0 && !string.IsNullOrWhiteSpace(boek.Isbn))
                {
                    var existingByIsbn = await db.Boeken.FirstOrDefaultAsync(b => b.Isbn == boek.Isbn);
                    if (existingByIsbn != null)
                    {
                        // update existing
                        existingByIsbn.Titel = boek.Titel;
                        existingByIsbn.Auteur = boek.Auteur;
                        existingByIsbn.CategorieID = boek.CategorieID;
                        existingByIsbn.Isbn = boek.Isbn;
                        db.Boeken.Update(existingByIsbn);
                        await db.SaveChangesAsync();
                        return;
                    }
                }

                if (boek.Id == 0) db.Boeken.Add(boek); else db.Boeken.Update(boek);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        public async Task SaveBoekenAsync(IEnumerable<Boek> boeken)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                await using var tx = await db.Database.BeginTransactionAsync();
                foreach (var b in boeken)
                {
                    if (b.Id == 0 && !string.IsNullOrWhiteSpace(b.Isbn))
                    {
                        var existingByIsbn = await db.Boeken.FirstOrDefaultAsync(x => x.Isbn == b.Isbn);
                        if (existingByIsbn != null)
                        {
                            db.Entry(existingByIsbn).CurrentValues.SetValues(b);
                            continue;
                        }
                    }

                    var existing = b.Id != 0 ? await db.Boeken.FindAsync(b.Id) : null;
                    if (existing == null) db.Boeken.Add(b); else db.Entry(existing).CurrentValues.SetValues(b);
                }
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        public async Task DeleteBoekAsync(int id)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Boeken.FindAsync(id);
                if (existing != null) { db.Boeken.Remove(existing); await db.SaveChangesAsync(); }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        // Leden
        public async Task<List<Lid>> GetLedenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Leden.AsNoTracking().ToListAsync();
        }

        public async Task SaveLidAsync(Lid lid)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                // If new and email provided, try to merge based on email
                if (lid.Id == 0 && !string.IsNullOrWhiteSpace(lid.Email))
                {
                    var existing = await db.Leden.FirstOrDefaultAsync(x => x.Email == lid.Email);
                    if (existing != null)
                    {
                        db.Entry(existing).CurrentValues.SetValues(lid);
                        await db.SaveChangesAsync();
                        return;
                    }
                }

                if (lid.Id == 0) db.Leden.Add(lid); else db.Leden.Update(lid);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        public async Task DeleteLidAsync(int id)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leden.FindAsync(id);
                if (existing != null) { db.Leden.Remove(existing); await db.SaveChangesAsync(); }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        // Uitleningen
        public async Task<List<Lenen>> GetUitleningenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Leningens.AsNoTracking().Include(l => l.Boek).Include(l => l.Lid).ToListAsync();
        }

        public async Task SaveUitleningAsync(Lenen uitlening)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                // validate related entities exist
                var boekExists = await db.Boeken.AnyAsync(b => b.Id == uitlening.BoekId);
                var lidExists = await db.Leden.AnyAsync(l => l.Id == uitlening.LidId);
                if (!boekExists || !lidExists)
                    throw new InvalidOperationException("Boek of Lid niet gevonden.");

                if (uitlening.Id == 0) db.Leningens.Add(uitlening); else db.Leningens.Update(uitlening);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        public async Task DeleteUitleningAsync(int id)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var existing = await db.Leningens.FindAsync(id);
                if (existing != null) { db.Leningens.Remove(existing); await db.SaveChangesAsync(); }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        // Categorien
        public async Task<List<Categorie>> GetCategorieenAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Categorien.AsNoTracking().Where(c => c.IsDeleted == false).OrderBy(c => c.Naam).ToListAsync();
        }

        public async Task SaveCategorieAsync(Categorie categorie)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                if (categorie.Id == 0 && !string.IsNullOrWhiteSpace(categorie.Naam))
                {
                    var existing = await db.Categorien.FirstOrDefaultAsync(x => x.Naam == categorie.Naam);
                    if (existing != null)
                    {
                        db.Entry(existing).CurrentValues.SetValues(categorie);
                        await db.SaveChangesAsync();
                        return;
                    }
                }

                if (categorie.Id == 0) db.Categorien.Add(categorie); else db.Categorien.Update(categorie);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        public async Task SaveCategorieenAsync(IEnumerable<Categorie> categorien)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                await using var tx = await db.Database.BeginTransactionAsync();
                foreach (var c in categorien)
                {
                    if (c.Id == 0 && !string.IsNullOrWhiteSpace(c.Naam))
                    {
                        var existingByName = await db.Categorien.FirstOrDefaultAsync(x => x.Naam == c.Naam);
                        if (existingByName != null)
                        {
                            db.Entry(existingByName).CurrentValues.SetValues(c);
                            continue;
                        }
                    }

                    var existing = c.Id != 0 ? await db.Categorien.FindAsync(c.Id) : null;
                    if (existing == null) db.Categorien.Add(c); else db.Entry(existing).CurrentValues.SetValues(c);
                }
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
                throw;
            }
        }

        public async Task DeleteCategorieAsync(int id)
        {
            using var db = _dbFactory.CreateDbContext();
            var existing = await db.Categorien.FindAsync(id);
            if (existing != null) { db.Categorien.Remove(existing); await db.SaveChangesAsync(); }
        }
    }
}