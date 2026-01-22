using System.Threading.Tasks;
using Biblio_App.Models;
using Biblio_Models.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Biblio_App.Services
{
    /// <summary>
    /// EfGegevensProvider
    /// 
    /// Concrete implementatie van IGegevensProvider
    /// die Entity Framework Core gebruikt om gegevens
    /// uit de lokale database (SQLite) op te halen.
    /// 
    /// Deze provider wordt typisch gebruikt voor:
    /// - dashboards
    /// - tellers (boeken / leden / open uitleningen)
    /// 
    /// Data-access logica zit hier,
    /// NIET in ViewModels of Views (MVVM).
    /// </summary>
    public class EfGegevensProvider : IGegevensProvider
    {
        // Factory om DbContext instanties te maken.
        // Wordt gebruikt i.p.v. een vaste DbContext
        // om threadingproblemen in MAUI te vermijden.
        private readonly IDbContextFactory<LocalDbContext> _dbFactory;

        /// <summary>
        /// Constructor – DbContextFactory wordt via Dependency Injection aangeleverd.
        /// </summary>
        public EfGegevensProvider(IDbContextFactory<LocalDbContext> dbFactory)
        {
            // Guard clause: DbContextFactory mag niet null zijn
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        /// <summary>
        /// Haalt dashboard-tellers op uit de lokale database.
        /// 
        /// Retourneert:
        /// - aantal boeken
        /// - aantal leden
        /// - aantal open uitleningen (niet ingeleverd)
        /// 
        /// Wordt asynchroon uitgevoerd om UI-blokkering te vermijden.
        /// </summary>
        public async Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync()
        {
            // Maak een nieuwe DbContext aan via de factory
            // 'using' zorgt ervoor dat de context correct wordt opgeruimd
            using var db = _dbFactory.CreateDbContext();

            // Tel het aantal boeken
            var boeken = await db.Boeken.CountAsync();

            // Tel het aantal leden
            var leden = await db.Leden.CountAsync();

            // Tel het aantal open uitleningen (ReturnedAt == null)
            var openUitleningen = await db.Leningens.CountAsync(l => l.ReturnedAt == null);

            // Tuple returnen (compact & duidelijk)
            return (boeken, leden, openUitleningen);
        }
    }
}
