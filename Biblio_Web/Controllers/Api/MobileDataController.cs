using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;

namespace Biblio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/mobiledata")]
    [AllowAnonymous]
    public class MobileDataController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        public MobileDataController(BiblioDbContext db) => _db = db;

        // GET: api/mobiledata
        // Returns categories, books, members and recent loans needed by the MAUI app in one call.
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Categories
            var categories = await _db.Categorien
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Naam)
                .Select(c => new { c.Id, c.Naam })
                .ToListAsync();

            // Books (include category name)
            var books = await _db.Boeken
                .Where(b => !b.IsDeleted)
                .Include(b => b.categorie)
                .OrderBy(b => b.Titel)
                .Select(b => new
                {
                    b.Id,
                    b.Titel,
                    b.Auteur,
                    b.Isbn,
                    CategorieId = b.CategorieID,
                    CategorieNaam = b.categorie != null ? b.categorie.Naam : null
                })
                .ToListAsync();

            // Members
            var members = await _db.Leden
                .Where(l => !l.IsDeleted)
                .OrderBy(l => l.Voornaam).ThenBy(l => l.AchterNaam)
                .Select(l => new { l.Id, l.Voornaam, l.AchterNaam, l.Email })
                .ToListAsync();

            // Recent loans (last 100) with book title and member name
            var loans = await _db.Leningens
                .Include(l => l.Boek)
                .Include(l => l.Lid)
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.StartDate)
                .Take(100)
                .Select(l => new
                {
                    l.Id,
                    l.BoekId,
                    BoekTitel = l.Boek != null ? l.Boek.Titel : null,
                    l.LidId,
                    LidNaam = l.Lid != null ? (l.Lid.Voornaam + " " + l.Lid.AchterNaam).Trim() : null,
                    l.StartDate,
                    l.DueDate,
                    l.ReturnedAt
                })
                .ToListAsync();

            var result = new
            {
                categories,
                books,
                members,
                loans
            };

            return Ok(result);
        }
    }
}
