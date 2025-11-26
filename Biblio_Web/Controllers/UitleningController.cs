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
        public async Task<IActionResult> Index(int? lidId, int? boekId, bool onlyOpen = false)
        {
            var q = _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).AsQueryable();
            if (lidId.HasValue) q = q.Where(l => l.LidId == lidId.Value);
            if (boekId.HasValue) q = q.Where(l => l.BoekId == boekId.Value);
            if (onlyOpen) q = q.Where(l => l.ReturnedAt == null);

            // populate filter lists for the view
            ViewBag.Leden = await _db.Leden.Where(l => !l.IsDeleted).OrderBy(l => l.Voornaam).ToListAsync();
            ViewBag.Boeken = await _db.Boeken.Where(b => !b.IsDeleted).OrderBy(b => b.Titel).ToListAsync();

            // pass selected ids back to view so the selected option can be marked
            ViewBag.SelectedLidId = lidId;
            ViewBag.SelectedBoekId = boekId;

            var list = await q.OrderByDescending(l => l.StartDate).ToListAsync();
            return View(list);
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

        // Details action: show one loan with related Boek and Lid
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var loan = await _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .FirstOrDefaultAsync(l => l.Id == id.Value && !l.IsDeleted);
            if (loan == null) return NotFound();
            return View(loan);
        }
    }
}
