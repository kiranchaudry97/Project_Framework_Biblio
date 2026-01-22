using System.Threading.Tasks;

namespace Biblio_App.Services
{
    /// <summary>
    /// IGegevensProvider
    /// 
    /// Servicecontract voor het ophalen van samenvattende gegevens
    /// (tellers / statistieken) uit de lokale databron.
    /// 
    /// Deze interface wordt typisch geïmplementeerd door:
    /// - een EF Core provider (LocalDbContext / SQLite)
    /// - eventueel later een API provider
    /// 
    /// Door een interface te gebruiken blijft de applicatie:
    /// - los gekoppeld
    /// - testbaar
    /// - uitbreidbaar
    /// </summary>
    public interface IGegevensProvider
    {
        /// <summary>
        /// Haalt de globale tellers op die gebruikt worden
        /// in dashboards of overzichtsschermen.
        /// 
        /// Retourneert een tuple met:
        /// - boeken: totaal aantal boeken
        /// - leden: totaal aantal leden
        /// - openUitleningen: aantal actieve (niet-ingeleverde) uitleningen
        /// 
        /// Async omdat de onderliggende implementatie
        /// database- of IO-gebonden is.
        /// </summary>
        Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync();
    }
}
