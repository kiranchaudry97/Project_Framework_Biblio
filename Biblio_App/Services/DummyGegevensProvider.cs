using System.Threading.Tasks;

namespace Biblio_App.Services
{
    // Eenvoudige dummy implementatie die statische waarden teruggeeft
    public class DummyGegevensProvider : IGegevensProvider
    {
        public Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync()
        {
            // In de toekomst: haal data op uit API of lokale database
            return Task.FromResult((boeken: 42, leden: 123, openUitleningen: 7));
        }
    }
}
