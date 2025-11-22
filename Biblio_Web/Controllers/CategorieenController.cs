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
    public class CategorieenController : Controller
    {
        private readonly BiblioDbContext _db;
        public CategorieenController(BiblioDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            return View(list);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var c = await _db.Categorien.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (c == null) return NotFound();
            return View(c);
        }

        [Authorize(Policy = "RequireStaff")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Categorie c)
        {
            if (ModelState.IsValid)
            {
                _db.Categorien.Add(c);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(c);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var c = await _db.Categorien.FindAsync(id.Value);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int id, Categorie c)
        {
            if (id != c.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                try
                {
                    _db.Categorien.Update(c);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _db.Categorien.AnyAsync(e => e.Id == c.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(c);
        }

        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var c = await _db.Categorien.FindAsync(id.Value);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var c = await _db.Categorien.FindAsync(id);
            if (c != null)
            {
                c.IsDeleted = true;
                _db.Categorien.Update(c);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
