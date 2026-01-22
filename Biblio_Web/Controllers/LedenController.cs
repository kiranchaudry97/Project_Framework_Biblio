using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;
using System.Linq;

namespace Biblio_Web.Controllers
{
    // MVC-controller voor ledenbeheer (Razor Views)
    // Standaard is elke actie beveiligd
    [Authorize]
    public class LedenController : Controller
    {
        // EF Core DbContext
        private readonly BiblioDbContext _db;

        // Dependency Injection van DbContext
        public LedenController(BiblioDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // INDEX
        // =====================================================
        // Publiek overzicht van leden met zoekfunctionaliteit
        [AllowAnonymous]
        public async Task<IActionResult> Index(string search)
        {
            // Start met een uitbreidbare query
            var query = _db.Leden.AsQueryable();

            // Zoekfilter (voornaam, achternaam of e-mail)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    (l.Voornaam ?? string.Empty).Contains(search) ||
                    (l.AchterNaam ?? string.Empty).Contains(search) ||
                    (l.Email ?? string.Empty).Contains(search)
                );
            }

            // Sorteer alfabetisch en voer query uit
            var leden = await query
                .OrderBy(l => l.Voornaam)
                .ThenBy(l => l.AchterNaam)
                .ToListAsync();

            return View(leden);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        // Publieke detailpagina van één lid
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            // Controleer of ID werd meegegeven
            if (id == null)
                return NotFound();

            // Zoek lid op basis van ID
            var lid = await _db.Leden.FindAsync(id.Value);

            // Lid niet gevonden
            if (lid == null)
                return NotFound();

            return View(lid);
        }

        // =====================================================
        // CREATE (GET)
        // =====================================================
        // Toont formulier om een nieuw lid aan te maken
        // Enkel toegankelijk voor Admin en Medewerker
        [Authorize(Policy = "RequireStaff")]
        public IActionResult Create()
        {
            return View();
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        // Verwerkt het aanmaken van een nieuw lid
        [HttpPost]
        [ValidateAntiForgeryToken] // Bescherming tegen CSRF
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Lid lid)
        {
            // Valideer input
            if (ModelState.IsValid)
            {
                // Voeg lid toe aan database
                _db.Leden.Add(lid);
                await _db.SaveChangesAsync();

                // Terug naar overzicht
                return RedirectToAction(nameof(Index));
            }

            // Bij fouten: formulier opnieuw tonen
            return View(lid);
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================
        // Toont formulier om een lid te bewerken
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var lid = await _db.Leden.FindAsync(id.Value);
            if (lid == null)
                return NotFound();

            return View(lid);
        }

        // =====================================================
        // EDIT (POST)
        // =====================================================
        // Verwerkt wijzigingen aan een lid
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int id, Lid lid)
        {
            // Beveiliging tegen ID-manipulatie
            if (id != lid.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                // Update lid
                _db.Leden.Update(lid);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(lid);
        }

        // =====================================================
        // DELETE (GET)
        // =====================================================
        // Toont bevestigingspagina voor verwijderen
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var lid = await _db.Leden.FindAsync(id.Value);
            if (lid == null)
                return NotFound();

            return View(lid);
        }

        // =====================================================
        // DELETE (POST)
        // =====================================================
        // Bevestigt het verwijderen (soft delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lid = await _db.Leden.FindAsync(id);

            if (lid != null)
            {
                // Soft delete: lid wordt niet fysiek verwijderd
                lid.IsDeleted = true;
                _db.Leden.Update(lid);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
