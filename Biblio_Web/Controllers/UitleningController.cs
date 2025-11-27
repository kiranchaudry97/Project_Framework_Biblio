using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace Biblio_Web.Controllers
{
    [Authorize]
    public class UitleningController : Controller
    {
        private readonly BiblioDbContext _db;
        public UitleningController(BiblioDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, int? categoryId, int? lidId, int? boekId, bool onlyOpen = false)
        {
            // provide categories for the category dropdown
            ViewBag.Categories = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.Search = search;

            // provide lists for member/book filters
            ViewBag.Leden = await _db.Leden.Where(l => !l.IsDeleted).OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
            ViewBag.Boeken = await _db.Boeken.Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();

            var q = _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => !l.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(l => (l.Boek != null) && (
                    l.Boek.Titel.Contains(search) ||
                    l.Boek.Auteur.Contains(search) ||
                    l.Boek.Isbn.Contains(search))
                );
            }

            if (categoryId.HasValue)
            {
                q = q.Where(l => l.Boek != null && l.Boek.CategorieID == categoryId.Value);
            }

            if (lidId.HasValue)
            {
                q = q.Where(l => l.LidId == lidId.Value);
                ViewBag.SelectedLid = lidId;
            }

            if (boekId.HasValue)
            {
                q = q.Where(l => l.BoekId == boekId.Value);
                ViewBag.SelectedBoek = boekId;
            }

            if (onlyOpen) q = q.Where(l => l.ReturnedAt == null);

            var list = await q.OrderByDescending(l => l.StartDate).ToListAsync();
            return View(list);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var loan = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).FirstOrDefaultAsync(l => l.Id == id.Value);
            if (loan == null) return NotFound();
            return View(loan);
        }

        [AllowAnonymous]
        public async Task<IActionResult> LatePartial()
        {
            var today = System.DateTime.Today;
            var list = await _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .Where(l => l.DueDate < today && l.ReturnedAt == null && !l.IsDeleted)
                .ToListAsync();

            return PartialView("_LateUitleningenPartial", list);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Leden = await _db.Leden.OrderBy(l => l.Voornaam).ToListAsync();
            ViewBag.Boeken = await _db.Boeken.OrderBy(b => b.Titel).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Lenen model)
        {
            if (ModelState.IsValid)
            {
                // check active loan
                var exists = await _db.Leningens.AnyAsync(l => l.BoekId == model.BoekId && l.ReturnedAt == null);
                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "Boek is al uitgeleend.");
                }
                else
                {
                    _db.Leningens.Add(model);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewBag.Leden = await _db.Leden.OrderBy(l => l.Voornaam).ToListAsync();
            ViewBag.Boeken = await _db.Boeken.OrderBy(b => b.Titel).ToListAsync();
            return View(model);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null) return NotFound();
            var loan = await _db.Leningens.FindAsync(id.Value);
            if (loan == null) return NotFound();
            loan.ReturnedAt = System.DateTime.Now;
            loan.IsClosed = true;
            _db.Leningens.Update(loan);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var loan = await _db.Leningens.Include(l=>l.Boek).Include(l=>l.Lid).FirstOrDefaultAsync(l => l.Id == id.Value);
            if (loan == null) return NotFound();
            return View(loan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _db.Leningens.FindAsync(id);
            if (loan != null)
            {
                loan.IsDeleted = true;
                _db.Leningens.Update(loan);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
