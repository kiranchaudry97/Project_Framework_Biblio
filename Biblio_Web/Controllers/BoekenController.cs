using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Biblio_Web.Controllers
{
    [Authorize]
    public class BoekenController : Controller
    {
        private readonly BiblioDbContext _db;

        public BoekenController(BiblioDbContext db)
        {
            _db = db;
        }

        // GET: Boeken
        [AllowAnonymous]
        public async Task<IActionResult> Index(string search, int? categoryId, int page = 1, int pageSize = 10)
        {
            // provide categories for the filter dropdown
            ViewBag.Categories = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.PageSize = pageSize;

            var q = _db.Boeken.Include(b => b.categorie).Where(b => !b.IsDeleted).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(b => b.Titel.Contains(search) || b.Auteur.Contains(search) || b.Isbn.Contains(search));
            }
            if (categoryId.HasValue)
            {
                q = q.Where(b => b.CategorieID == categoryId.Value);
            }

            var totalCount = await q.CountAsync();
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var list = await q.OrderBy(b => b.Titel)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Search = search;

            return View(list);
        }

        // GET: Boeken/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var boek = await _db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == id.Value);
            if (boek == null) return NotFound();
            return View(boek);
        }

        // GET: Boeken/Create
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            return View();
        }

        // POST: Boeken/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Create(Boek boek)
        {
            if (ModelState.IsValid)
            {
                _db.Boeken.Add(boek);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            return View(boek);
        }

        // GET: Boeken/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var boek = await _db.Boeken.FindAsync(id.Value);
            if (boek == null) return NotFound();
            ViewBag.Categories = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            return View(boek);
        }

        // POST: Boeken/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Edit(int id, Boek boek)
        {
            if (id != boek.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                try
                {
                    _db.Boeken.Update(boek);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _db.Boeken.AnyAsync(e => e.Id == boek.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _db.Categorien.OrderBy(c => c.Naam).ToListAsync();
            return View(boek);
        }

        // GET: Boeken/Delete/5
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var boek = await _db.Boeken.FirstOrDefaultAsync(b => b.Id == id.Value);
            if (boek == null) return NotFound();
            return View(boek);
        }

        // POST: Boeken/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var boek = await _db.Boeken.FindAsync(id);
            if (boek != null)
            {
                // soft delete
                boek.IsDeleted = true;
                _db.Boeken.Update(boek);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
