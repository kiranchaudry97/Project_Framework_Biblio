/*
API endpoints (CategorieenApiController)
- GET  /api/categorieen            -> list categories (RequireStaff)
- GET  /api/categorieen/{id}       -> get category detail (RequireStaff)
- POST /api/categorieen            -> create category (RequireStaff)
- PUT  /api/categorieen/{id}       -> update category (RequireStaff)
- DELETE /api/categorieen/{id}     -> soft-delete category (RequireStaff)
*/

// CategorieenApiController.cs
// Doel: API-controller voor het beheren van categorieën (CRUD) via JSON‑endpoints.
// Gebruik: biedt beveiligde endpoints voor medewerkers/administrators om categorieën te lezen, aan te maken,
//         bij te werken en zacht te verwijderen (soft‑delete). Wordt gebruikt door de admin UI of externe clients.
// Doelstellingen:
// - Bied RESTful en veilige endpoints voor categoriebeheer, met duidelijke HTTP‑statuscodes bij fouten.
// - Gebruik soft‑delete zodat verwijderde categorieën niet direct fysiek worden verwijderd maar niet meer zichtbaar zijn.
// - Valideer input en retourneer ProblemDetails/ValidationProblemDetails voor consistente foutafhandeling.
// - Beperk toegang via de `RequireStaff`-policy zodat alleen bevoegde gebruikers deze API kunnen gebruiken.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/categorieen")]
    [Authorize(Policy = "RequireStaff")]
    public class CategorieenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        public CategorieenApiController(BiblioDbContext db) => _db = db;

        // GET: api/categorieen
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var list = await _db.Categorien.Where(c => !c.IsDeleted).ToListAsync();
            return Ok(list);
        }

        // GET: api/categorieen/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var c = await _db.Categorien.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (c == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Category not found" });
            return Ok(c);
        }

        // POST: api/categorieen
        [HttpPost]
        public async Task<IActionResult> Post(Categorie model)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            var entity = new Categorie
            {
                Naam = model.Naam
            };
            _db.Categorien.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
        }

        // PUT: api/categorieen/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Categorie model)
        {
            if (id != model.Id) return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Id mismatch" });
            var existing = await _db.Categorien.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Category not found" });

            existing.Naam = model.Naam;
            _db.Categorien.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/categorieen/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Categorien.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Category not found" });
            existing.IsDeleted = true;
            _db.Categorien.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
