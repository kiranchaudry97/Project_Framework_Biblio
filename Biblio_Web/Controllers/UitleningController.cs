using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;
using System.Linq;

namespace Biblio_Web.Controllers
{
    // MVC-controller voor het beheren van uitleningen (lenen)
    // Standaard moet de gebruiker ingelogd zijn
    [Authorize]
    public class UitleningController : Controller
    {
        // EF Core DbContext
        private readonly BiblioDbContext _db;

        // Dependency Injection van DbContext
        public UitleningController(BiblioDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // INDEX
        // =====================================================
        // Publiek overzicht van uitleningen met filters:
        // - zoekterm
        // - categorie
        // - lid
        // - boek
        // - alleen open leningen
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            int? lidId,
            int? boekId,
            bool onlyOpen = false)
        {
            // ---------------------------------------------
            // Dropdowns en filterdata voor de view
            // ---------------------------------------------

            // Categorieën voor filter
            ViewBag.Categories = await _db.Categorien
                .OrderBy(c => c.Naam)
                .ToListAsync();

            ViewBag.SelectedCategory = categoryId;
            ViewBag.Search = search;

            // Leden voor filter
            ViewBag.Leden = await _db.Leden
                .Where(l => !l.IsDeleted)
                .OrderBy(l => l.Voornaam)
                .ThenBy(l => l.AchterNaam)
                .ToListAsync();

            // Boeken voor filter
            ViewBag.Boeken = await _db.Boeken
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Titel)
                .ToListAsync();

            // ---------------------------------------------
            // Basisquery: alle niet-verwijderde leningen
            // ---------------------------------------------
            var query = _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .Where(l => !l.IsDeleted)
                .AsQueryable();

            // ---------------------------------------------
            // Zoekfilter (titel, auteur of ISBN)
            // ---------------------------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    l.Boek != null &&
                    (
                        l.Boek.Titel.Contains(search) ||
                        l.Boek.Auteur.Contains(search) ||
                        l.Boek.Isbn.Contains(search)
                    )
                );
            }

            // ---------------------------------------------
            // Filter op categorie
            // ---------------------------------------------
            if (categoryId.HasValue)
            {
                query = query.Where(l =>
                    l.Boek != null &&
                    l.Boek.CategorieID == categoryId.Value);
            }

            // ---------------------------------------------
            // Filter op lid
            // ---------------------------------------------
            if (lidId.HasValue)
            {
                query = query.Where(l => l.LidId == lidId.Value);
                ViewBag.SelectedLid = lidId;
            }

            // ---------------------------------------------
            // Filter op boek
            // ---------------------------------------------
            if (boekId.HasValue)
            {
                query = query.Where(l => l.BoekId == boekId.Value);
                ViewBag.SelectedBoek = boekId;
            }

            // ---------------------------------------------
            // Enkel open leningen (nog niet ingeleverd)
            // ---------------------------------------------
            if (onlyOpen)
            {
                query = query.Where(l => l.ReturnedAt == null);
            }

            // ---------------------------------------------
            // Resultaat sorteren en ophalen
            // ---------------------------------------------
            var list = await query
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return View(list);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        // Publieke detailpagina van één uitlening
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .FirstOrDefaultAsync(l => l.Id == id.Value);

            if (loan == null)
                return NotFound();

            return View(loan);
        }

        // =====================================================
        // LATE PARTIAL
        // =====================================================
        // Partial view met te laat ingeleverde boeken
        // Wordt bv. gebruikt in dashboard of sidebar
        [AllowAnonymous]
        public async Task<IActionResult> LatePartial()
        {
            var today = System.DateTime.Today;

            var list = await _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .Where(l =>
                    !l.IsDeleted &&
                    l.ReturnedAt == null &&
                    l.DueDate < today)
                .ToListAsync();

            return PartialView("_LateUitleningenPartial", list);
        }

        // =====================================================
        // CREATE (GET)
        // =====================================================
        // Toont formulier om een nieuwe uitlening te maken
        // Enkel Admin / Medewerker
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Leden = await _db.Leden
                .OrderBy(l => l.Voornaam)
                .ToListAsync();

            ViewBag.Boeken = await _db.Boeken
                .OrderBy(b => b.Titel)
                .ToListAsync();

            return View();
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        // Verwerkt het aanmaken van een nieuwe uitlening
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Lenen model)
        {
            if (ModelState.IsValid)
            {
                // Controle: boek mag niet al actief uitgeleend zijn
                var exists = await _db.Leningens
                    .AnyAsync(l =>
                        l.BoekId == model.BoekId &&
                        l.ReturnedAt == null);

                if (exists)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Boek is al uitgeleend."
                    );
                }
                else
                {
                    _db.Leningens.Add(model);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            // Bij fouten: dropdowns opnieuw vullen
            ViewBag.Leden = await _db.Leden
                .OrderBy(l => l.Voornaam)
                .ToListAsync();

            ViewBag.Boeken = await _db.Boeken
                .OrderBy(b => b.Titel)
                .ToListAsync();

            return View(model);
        }

        // =====================================================
        // RETURN
        // =====================================================
        // Markeert een uitlening als teruggebracht
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _db.Leningens.FindAsync(id.Value);
            if (loan == null)
                return NotFound();

            // Sluit de uitlening af
            loan.ReturnedAt = System.DateTime.Now;
            loan.IsClosed = true;

            _db.Leningens.Update(loan);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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

            var loan = await _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .FirstOrDefaultAsync(l => l.Id == id.Value);

            if (loan == null)
                return NotFound();

            return View(loan);
        }

        // =====================================================
        // DELETE (POST)
        // =====================================================
        // Soft delete van een uitlening
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _db.Leningens.FindAsync(id);

            if (loan != null)
            {
                // Soft delete: record blijft in database
                loan.IsDeleted = true;
                _db.Leningens.Update(loan);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
