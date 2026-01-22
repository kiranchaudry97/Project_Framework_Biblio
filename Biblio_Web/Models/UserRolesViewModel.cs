using System.Collections.Generic;

namespace Biblio_Web.Models
{
    // ViewModel gebruikt om de rollen van een gebruiker te tonen
    // Bevat enkel leesbare informatie
    public class UserRolesViewModel
    {
        // Gebruikersnaam of e-mailadres
        // Wordt getoond ter identificatie van de gebruiker
        public string UserName { get; set; } = string.Empty;

        // Lijst van rol-namen die aan de gebruiker zijn toegekend
        // Bijvoorbeeld: Admin, Medewerker, Lid
        public List<string> Roles { get; set; } = new List<string>();
    }
}
