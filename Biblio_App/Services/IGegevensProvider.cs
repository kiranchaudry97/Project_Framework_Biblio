using System.Threading.Tasks;

namespace Biblio_App.Services
{
    // Service contract for retrieving aggregate counters (local EF provider)
    public interface IGegevensProvider
    {
        Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync();
    }
}