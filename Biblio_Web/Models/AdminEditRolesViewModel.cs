using System.Collections.Generic;

namespace Biblio_Web.Models
{
    // ViewModel gebruikt door een admin om rollen van een gebruiker te beheren
    // Combineert gebruikersinformatie met een lijst van rol-checkboxes
    public class AdminEditRolesViewModel
    {
        // Unieke ID van de gebruiker (Identity)
        // Nodig om rollen correct toe te wijzen
        public string UserId { get; set; } = string.Empty;

        // E-mailadres van de gebruiker
        // Wordt enkel getoond ter informatie
        public string Email { get; set; } = string.Empty;

        // Lijst van alle rollen met bijhorende checkboxstatus
        public List<RoleCheckbox> Roles { get; set; } = new();
    }

    // Helperklasse voor het weergeven van rollen als checkboxes
    public class RoleCheckbox
    {
        // Naam van de rol (bv. Admin, Medewerker, Lid)
        public string RoleName { get; set; } = string.Empty;

        // Geeft aan of de rol momenteel aan de gebruiker is toegewezen
        public bool IsSelected { get; set; }
    }
}
