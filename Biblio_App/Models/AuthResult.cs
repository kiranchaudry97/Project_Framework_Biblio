using System;

namespace Biblio_App.Models
{
    // AuthResult
    // DTO die het resultaat van een authenticatie-aanroep
    // (bv. login via API) voorstelt.
    // Wordt gebruikt door de MAUI app om te bepalen of
    // de gebruiker succesvol is aangemeld.
    public class AuthResult
    {
        // JWT access token dat gebruikt wordt
        // voor geauthenticeerde API-aanroepen
        public string? AccessToken { get; set; }

        // Optional refresh token om een nieuw access token
        // op te vragen zonder opnieuw in te loggen
        public string? RefreshToken { get; set; }

        // Vervaldatum van het access token
        // Wordt gebruikt om tijdig te vernieuwen
        public DateTime? Expires { get; set; }

        // Convenience property:
        // Geeft true terug als de login geslaagd is
        // (er is een geldig access token)
        public bool Success => !string.IsNullOrEmpty(AccessToken);
    }
}
