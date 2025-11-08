// Patronen aanwezig in dit bestand:
// - Uitgebreide IdentityUser met extra properties (FullName, IsBlocked)
// - Wordt gebruikt door Identity voor authenticatie/autoristatie
// - Flag `IsBlocked` wordt door UI/API gecontroleerd om login/token-uitgifte te blokkeren

// Doel: ASP.NET Identity gebruiker met extra veld FullName.
// Beschrijving: Uitgebreide IdentityUser voor desktop; FullName wordt o.a. in profiel en beheer UI gebruikt.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;

namespace Biblio_Models.Entiteiten
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; } // verplichte extra property
        // Nieuw: markeer accounts als geblokkeerd (blokkeer login wanneer true)
        public bool IsBlocked { get; set; } = false;
    }
}
