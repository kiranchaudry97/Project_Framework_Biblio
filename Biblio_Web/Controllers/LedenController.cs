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
    public class LedenController : Controller
    {
        private readonly BiblioDbContext _db;
        public LedenController(BiblioDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index(string search)
        {
            var q = _db.Leden.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(l => (l.Voornaam ?? string.Empty).Contains(search) || (l.AchterNaam ?? string.Empty).Contains(search) || (l.Email ?? string.Empty).Contains(search));
            }
            var list = await q.OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam).ToListAsync();
            return View(list);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var lid = await _db.Leden.FindAsync(id.Value);
            if (lid == null) return NotFound();
            return View(lid);
        }

        [Authorize(Policy = "RequireStaff")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Lid lid)
        {
            if (ModelState.IsValid)
            {
                _db.Leden.Add(lid);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lid);
        }

        // GET: Leden/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var lid = await _db.Leden.FindAsync(id.Value);
            if (lid == null) return NotFound();
            return View(lid);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int id, Lid lid)
        {
            if (id != lid.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                _db.Leden.Update(lid);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lid);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var lid = await _db.Leden.FindAsync(id.Value);
            if (lid == null) return NotFound();
            return View(lid);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lid = await _db.Leden.FindAsync(id);
            if (lid != null)
            {
                lid.IsDeleted = true;
                _db.Leden.Update(lid);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
