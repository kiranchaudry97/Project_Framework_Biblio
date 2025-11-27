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
    [ApiController]
    [Route("api/diagnostics")]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public DiagnosticsController(BiblioDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        /// <summary>
        /// Uitvoeren van een smoke-test: controle op DB-connectie, rollen, admin-gebruiker en een kleine
        /// create/delete operatie op boeken, plus het tellen van achterstallige leningen.
        /// Retourneert een JSON-object met samenvattende flags en eventuele foutmeldingen.
        /// </summary>
        [HttpGet("smoke")]
        public async Task<IActionResult> SmokeTest()
        {
            var result = new
            {
                timestamp = DateTime.UtcNow,
                canConnect = false,
                roles = new { Admin = false, Medewerker = false, Lid = false },
                adminUserExists = false,
                adminPasswordValid = false,
                booksCrud = new { create = false, delete = false, message = "" },
                overdueLoans = 0,
                errors = Array.Empty<string>()
            };

            var errors = new System.Collections.Generic.List<string>();
            bool canConnect = false;
            try
            {
                canConnect = await _db.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                errors.Add("DB connect error: " + ex.Message);
            }

            bool roleAdmin = false, roleStaff = false, roleMember = false;
            try
            {
                roleAdmin = await _roleManager.RoleExistsAsync("Admin");
                roleStaff = await _roleManager.RoleExistsAsync("Medewerker");
                roleMember = await _roleManager.RoleExistsAsync("Lid");
            }
            catch (Exception ex)
            {
                errors.Add("Role check error: " + ex.Message);
            }

            bool adminExists = false, adminPwdValid = false;
            try
            {
                var adminEmail = _config["Seed:AdminEmail"] ?? "admin@biblio.local";
                var adminPwd = _config["Seed:AdminPassword"] ?? "Admin123!";
                var admin = await _userManager.FindByEmailAsync(adminEmail);
                if (admin != null)
                {
                    adminExists = true;
                    adminPwdValid = await _userManager.CheckPasswordAsync(admin, adminPwd);
                }
            }
            catch (Exception ex)
            {
                errors.Add("Admin user check error: " + ex.Message);
            }

            // Books CRUD smoke: create and delete a temporary book
            bool created = false, deleted = false;
            string bookMsg = string.Empty;
            try
            {
                // find any category
                var cat = await _db.Categorien.FirstOrDefaultAsync();
                if (cat == null)
                {
                    cat = new Categorie { Naam = "smoke-cat-" + Guid.NewGuid().ToString().Substring(0, 6) };
                    _db.Categorien.Add(cat);
                    await _db.SaveChangesAsync();
                }

                var testTitle = "smoke-test-" + Guid.NewGuid().ToString();
                var b = new Boek { Titel = testTitle, Auteur = "Smoke", Isbn = "SMOKE" + DateTime.UtcNow.Ticks.ToString().Substring(0,6), CategorieID = cat.Id };
                _db.Boeken.Add(b);
                await _db.SaveChangesAsync();
                created = b.Id > 0;

                // now delete (hard-delete) to clean up
                if (created)
                {
                    _db.Boeken.Remove(b);
                    await _db.SaveChangesAsync();
                    deleted = true;
                }
            }
            catch (Exception ex)
            {
                bookMsg = ex.Message;
                errors.Add("Books CRUD error: " + ex.Message);
            }

            int overdueCount = 0;
            try
            {
                overdueCount = await _db.Leningens.Where(l => !l.IsDeleted && l.ReturnedAt == null && l.DueDate < DateTime.Today).CountAsync();
            }
            catch (Exception ex)
            {
                errors.Add("Overdue query error: " + ex.Message);
            }

            var response = new
            {
                timestamp = DateTime.UtcNow,
                canConnect = canConnect,
                roles = new { Admin = roleAdmin, Medewerker = roleStaff, Lid = roleMember },
                adminUserExists = adminExists,
                adminPasswordValid = adminPwdValid,
                booksCrud = new { create = created, delete = deleted, message = bookMsg },
                overdueLoans = overdueCount,
                errors = errors.ToArray()
            };

            return Ok(response);
        }
    }
}
