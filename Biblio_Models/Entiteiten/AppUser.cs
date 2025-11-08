// 1) //LINQ - AppUser wordt gebruikt in queries via UserManager/DbContext
//    Waar: user-lookup en role checks in seeding en administratie (bijv. `FindByEmailAsync`, `GetRolesAsync`).
//    Doel: gebruikers zoeken/filteren.
// 2) //lambda-expressie - predicaten/selectors kunnen AppUser gebruiken
//    Waar: in LINQ/selects en error messages (`Errors.Select(e => e.Description)`).
//    Doel: compacte selectie en mapping van foutteksten of filters.
// 3) //CRUD - AppUser neemt deel aan Create/Update operaties via Identity/UserManager
//    Waar: `CreateAsync`, `UpdateAsync`, `AddToRoleAsync`, `DeleteAsync`.
//    Doel: beheren van gebruikersaccounts en rollen.
// - Uitgebreide IdentityUser met extra properties (FullName, IsBlocked)
// - Wordt gebruikt door Identity voor authenticatie/authorisatie
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
