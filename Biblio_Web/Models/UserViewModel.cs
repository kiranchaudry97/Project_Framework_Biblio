using System.Collections.Generic;

namespace Biblio_Web.Models
{
    // ViewModel gebruikt voor het admin-overzicht van gebruikers
    // Bevat samengevatte informatie per gebruiker
    public class UserViewModel
    {
        // Unieke ID van de gebruiker (Identity)
        public string Id { get; set; } = string.Empty;

        // Gebruikersnaam (of e-mailadres)
        public string UserName { get; set; } = string.Empty;

        // E-mailadres van de gebruiker
        public string Email { get; set; } = string.Empty;

        // Geeft aan of de gebruiker geblokkeerd is
        // True = geblokkeerd, false = actief
        public bool Blocked { get; set; }

        // Rollen die aan de gebruiker zijn toegekend
        // Bijvoorbeeld: Admin, Medewerker, Lid
        public List<string> Roles { get; set; } = new List<string>();
    }
}
