/*
API endpoints (BoekenApiController)
- GET  /api/boeken?page=1&pageSize=20      -> paged list (RequireMember)
- GET  /api/boeken/{id}                   -> get book detail (RequireMember)
- POST /api/boeken                        -> create book (RequireStaff)
- PUT  /api/boeken/{id}                   -> update book (RequireStaff)
- DELETE /api/boeken/{id}                 -> soft-delete book (RequireStaff)
*/

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Biblio_Web.Models.ApiDtos;

namespace Biblio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/boeken")]
    [Authorize(Policy = "RequireMember")]
    public class BoekenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        public BoekenApiController(BiblioDbContext db) => _db = db;

        // GET: api/Boeken?page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var q = _db.Boeken.Where(b => !b.IsDeleted).Include(b => b.categorie).AsQueryable();
            var total = await q.CountAsync();
            var totalPages = (int)System.Math.Ceiling(total / (double)pageSize);
            var items = await q.OrderBy(b => b.Titel).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // map to DTOs
            var dtoItems = items.Select(b => new BoekDto
            {
                Id = b.Id,
                Titel = b.Titel,
                Auteur = b.Auteur,
                Isbn = b.Isbn,
                CategorieID = b.CategorieID,
                CategorieNaam = b.categorie == null ? string.Empty : b.categorie.Naam
            }).ToList();

            var result = new
            {
                page,
                pageSize,
                total,
                totalPages,
                items = dtoItems
            };

            return Ok(result);
        }

        // GET: api/Boeken/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var boek = await _db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (boek == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Book not found" });

            var dto = new BoekDto
            {
                Id = boek.Id,
                Titel = boek.Titel,
                Auteur = boek.Auteur,
                Isbn = boek.Isbn,
                CategorieID = boek.CategorieID,
                CategorieNaam = boek.categorie == null ? string.Empty : boek.categorie.Naam
            };

            return Ok(dto);
        }

        // POST: api/Boeken
        [HttpPost]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Post(Boek model)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            var entity = new Boek
            {
                Titel = model.Titel,
                Auteur = model.Auteur,
                Isbn = model.Isbn,
                CategorieID = model.CategorieID
            };
            _db.Boeken.Add(entity);
            await _db.SaveChangesAsync();

            var saved = await _db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == entity.Id);

            var dto = new BoekDto
            {
                Id = saved.Id,
                Titel = saved.Titel,
                Auteur = saved.Auteur,
                Isbn = saved.Isbn,
                CategorieID = saved.CategorieID,
                CategorieNaam = saved.categorie == null ? string.Empty : saved.categorie.Naam
            };

            return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
        }

        // PUT: api/Boeken/5
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Put(int id, Boek model)
        {
            if (id != model.Id) return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Id mismatch" });
            var existing = await _db.Boeken.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Book not found" });

            existing.Titel = model.Titel;
            existing.Auteur = model.Auteur;
            existing.Isbn = model.Isbn;
            existing.CategorieID = model.CategorieID;

            _db.Boeken.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Boeken/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Boeken.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Book not found" });
            existing.IsDeleted = true;
            _db.Boeken.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
