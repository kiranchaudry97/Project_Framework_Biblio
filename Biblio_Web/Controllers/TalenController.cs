using Microsoft.AspNetCore.Mvc;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace Biblio_Web.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class TalenController : Controller
    {
        private readonly BiblioDbContext _db;
        private readonly ILogger<TalenController> _logger;

        public TalenController(BiblioDbContext db, ILogger<TalenController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var items = await _db.Talen.Where(t => !t.IsDeleted).ToListAsync();
                return View(items);
            }
            catch (Exception ex)
            {
                // Common cause: database schema not up-to-date (missing IsDefault/IsDeleted column)
                _logger.LogError(ex, "Failed to load languages. This can happen when the database schema is outdated.");

                // Provide a helpful message in UI and return an empty list to avoid app crash.
                TempData["ErrorMessage"] = "Database schema mismatch: missing language columns. Run EF migrations (dotnet ef database update) or check the database.";
                return View(new System.Collections.Generic.List<Taal>());
            }
        }

        public IActionResult Create() => View(new Taal());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Taal model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Code = model.Code?.Trim() ?? string.Empty;

            // ensure unique code
            var exists = await _db.Talen.AnyAsync(t => !t.IsDeleted && t.Code.Equals(model.Code, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                ModelState.AddModelError("Code", "Er bestaat al een taal met deze code.");
                return View(model);
            }

            // if this one is marked default, clear other defaults
            if (model.IsDefault)
            {
                var defaults = await _db.Talen.Where(t => t.IsDefault && !t.IsDeleted).ToListAsync();
                foreach (var d in defaults)
                {
                    d.IsDefault = false;
                    _db.Talen.Update(d);
                }
            }

            _db.Talen.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var t = await _db.Talen.FindAsync(id);
            if (t == null) return NotFound();
            return View(t);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Taal model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Code = model.Code?.Trim() ?? string.Empty;

            // ensure unique code (excluding current)
            var exists = await _db.Talen.AnyAsync(t => !t.IsDeleted && t.Id != model.Id && t.Code.Equals(model.Code, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                ModelState.AddModelError("Code", "Er bestaat al een taal met deze code.");
                return View(model);
            }

            if (model.IsDefault)
            {
                var defaults = await _db.Talen.Where(t => t.IsDefault && !t.IsDeleted && t.Id != model.Id).ToListAsync();
                foreach (var d in defaults)
                {
                    d.IsDefault = false;
                    _db.Talen.Update(d);
                }
            }

            _db.Talen.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Talen.FindAsync(id);
            if (t == null) return NotFound();
            return View(t);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var t = await _db.Talen.FindAsync(id);
            if (t == null) return NotFound();

            // soft-delete
            var wasDefault = t.IsDefault;
            t.IsDeleted = true;
            t.IsDefault = false;
            _db.Talen.Update(t);
            await _db.SaveChangesAsync();

            if (wasDefault)
            {
                // pick another language to be default if available
                var other = await _db.Talen.Where(x => !x.IsDeleted).FirstOrDefaultAsync();
                if (other != null)
                {
                    other.IsDefault = true;
                    _db.Talen.Update(other);
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
