using System.Collections.Generic;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Services
{
    public interface ILocalRepository
    {
        // Boeken
        Task<List<Boek>> GetBoekenAsync();
        Task SaveBoekAsync(Boek boek);
        Task SaveBoekenAsync(IEnumerable<Boek> boeken);
        Task DeleteBoekAsync(int id);

        // Leden
        Task<List<Lid>> GetLedenAsync();
        Task SaveLidAsync(Lid lid);
        Task DeleteLidAsync(int id);

        // Uitleningen
        Task<List<Lenen>> GetUitleningenAsync();
        Task SaveUitleningAsync(Lenen uitlening);
        Task DeleteUitleningAsync(int id);

        // Categorien
        Task<List<Categorie>> GetCategorieenAsync();
        Task SaveCategorieAsync(Categorie categorie);
        Task SaveCategorieenAsync(IEnumerable<Categorie> categorien);
        Task DeleteCategorieAsync(int id);
    }
}
