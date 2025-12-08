using System.Threading.Tasks;
using Biblio_App.Models;

namespace Biblio_App.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        void Logout();
        string? GetToken();
    }
}