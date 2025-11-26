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
    public class TalenController : Controller
    {
        private readonly BiblioDbContext _db;
        public TalenController(BiblioDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Talen.OrderBy(t => t.Naam).ToListAsync();
            return View(list);
        }

        [Authorize(Policy = "RequireStaff")]
        public IActionResult Create() => View(new Taal());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Taal t)
        {
            if (ModelState.IsValid)
            {
                _db.Talen.Add(t);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(t);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var t = await _db.Talen.FindAsync(id.Value);
            if (t == null) return NotFound();
            return View(t);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int id, Taal t)
        {
            if (id != t.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                try
                {
                    _db.Talen.Update(t);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _db.Talen.AnyAsync(e => e.Id == t.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(t);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var t = await _db.Talen.FindAsync(id.Value);
            if (t == null) return NotFound();
            return View(t);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var t = await _db.Talen.FindAsync(id);
            if (t != null)
            {
                t.IsActive = false;
                _db.Talen.Update(t);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
