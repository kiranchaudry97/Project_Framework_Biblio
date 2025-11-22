using System.Threading.Tasks;

namespace Biblio_App.Services
{
    // Interface voor het ophalen van globale tellerwaarden (boeken, leden, open uitleningen)
    public interface IGegevensProvider
    {
        Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync();
    }
}
