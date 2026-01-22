using System.Threading.Tasks;
using Biblio_App.Models;

namespace Biblio_App.Services
{
    /// <summary>
    /// IAuthService
    /// 
    /// Contract (interface) voor authenticatie binnen de MAUI app.
    /// 
    /// Deze interface definieert WAT er mogelijk is,
    /// niet HOE het geïmplementeerd wordt.
    /// 
    /// Concreet:
    /// - inloggen via API
    /// - uitloggen
    /// - ophalen van JWT access token
    /// - vernieuwen van tokens via refresh token
    /// 
    /// Wordt gebruikt door ViewModels,
    /// zodat deze los blijven van concrete implementaties.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Probeert een gebruiker in te loggen met e-mail en wachtwoord.
        /// 
        /// Retourneert een AuthResult met:
        /// - AccessToken (JWT)
        /// - RefreshToken
        /// - Expiry informatie
        /// 
        /// Bij mislukking is Success = false.
        /// </summary>
        Task<AuthResult> LoginAsync(string email, string password);

        /// <summary>
        /// Logt de huidige gebruiker uit.
        /// 
        /// Verwijdert:
        /// - in-memory token
        /// - opgeslagen tokens in SecureStorage
        /// 
        /// Heeft geen returnwaarde omdat logout
        /// altijd lokaal wordt afgehandeld.
        /// </summary>
        void Logout();

        /// <summary>
        /// Geeft het huidige access token terug.
        /// 
        /// Wordt gebruikt door:
        /// - HttpMessageHandler
        /// - API service calls
        /// 
        /// Retourneert null wanneer de gebruiker
        /// niet (meer) is aangemeld.
        /// </summary>
        string? GetToken();

        /// <summary>
        /// Vraagt een nieuw access token aan
        /// op basis van een refresh token.
        /// 
        /// Wordt gebruikt wanneer:
        /// - access token verlopen is
        /// - API een 401 Unauthorized terugstuurt
        /// </summary>
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
    }
}
