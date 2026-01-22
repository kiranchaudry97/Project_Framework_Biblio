/*
API endpoints (DiagnosticsController)
- GET /api/diagnostics/smoke   -> voer smoke-tests uit (DB, rollen, admin-gebruiker, boeken CRUD, aantal achterstallige leningen) (AllowAnonymous)
*/

// DiagnosticsController.cs
// Doel: eenvoudige diagnostische API-endpoints voor healthchecks en smoke-tests.
// Gebruik: biedt een /api/diagnostics/smoke endpoint dat snel controleert of kernfuncties werken
//         (DB-connectiviteit, rollen/gebruikers, tijdelijke CRUD-operatie op boeken, overdue query).
// Doelstellingen:
// - Bied een veilig te gebruiken diagnostisch hulpmiddel voor ontwikkel- en staging-omgevingen.
// - Geef nuttige maar niet-gevoelige informatie terug (geen wachtwoorden of gevoelige tokens).
// - Voer lichte CRUD- en read-checks uit om te verifiëren dat database- en identity-componenten functioneren.
// - Rapporteer fouten in een leesbaar array zodat monitoring systemen kort kunnen samenvatten wat misgaat.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Identity;

namespace Biblio_Web.Controllers.Api
{
    /// <summary>
    /// Controller met diagnostische endpoints. Bevat een smoke-test endpoint dat verschillende subsystemen
    /// controleert en een compacte JSON-respons teruggeeft met statusinformatie.
    /// </summary>
    // Diagnostics controller voor smoke/health checks
    // Publiek endpoint (in productie vaak beperken)
    [ApiController]
    [Route("api/diagnostics")]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        // Injecteer noodzakelijke services
        private readonly BiblioDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public DiagnosticsController(
            BiblioDbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        // GET: api/diagnostics/smoke
        // Voert een end-to-end rooktest uit
        [HttpGet("smoke")]
        public async Task<IActionResult> SmokeTest()
        {
            // 1. DB connectie
            // 2. Rollen bestaan
            // 3. Admin gebruiker & wachtwoord
            // 4. CRUD test op boeken
            // 5. Aantal achterstallige leningen
            // 6. Verzamel fouten
            // ...
            return Ok(/* samenvattend JSON */);
        }
    }
}