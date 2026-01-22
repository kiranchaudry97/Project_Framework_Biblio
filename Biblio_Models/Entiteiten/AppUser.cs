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

// using system om te laten importeren zoals in andere bestanden om consistent te zijn en gebruik van dezelfde namespaces mogelijk te maken
// om klasse te gebruiken en fundamentele functionaliteit te vo



using System; // using directives voor benodigde namespaces
using System.Collections.Generic; // generieke collecties voor lijsten
using System.Linq; // LINQ functionaliteit voor queries
using System.Text; // tekstmanipulatie
using System.Threading.Tasks; // asynchrone taken

using Microsoft.AspNetCore.Identity; // IdentityUser voor ASP.NET Identity functionaliteit

namespace Biblio_Models.Entiteiten // namespace voor entiteiten in Biblio_Models
{
    public class AppUser : IdentityUser // AppUser klasse die IdentityUser uitbreidt
    {
        public string? FullName { get; set; } // verplichte extra property
        // Nieuw: markeer accounts als geblokkeerd (blokkeer login wanneer true)
        public bool IsBlocked { get; set; } = false; // standaard niet geblokkeerd
    }
}
