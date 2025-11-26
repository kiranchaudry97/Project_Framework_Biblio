using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/boeken")]
    [Authorize(Policy = "RequireMember")]
    public class BoekenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        public BoekenApiController(BiblioDbContext db) => _db = db;

        // GET: api/Boeken
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Boek>>> Get()
        {
            var list = await _db.Boeken.Where(b => !b.IsDeleted).Include(b => b.categorie).ToListAsync();
            return Ok(list);
        }

        // GET: api/Boeken/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Boek>> Get(int id)
        {
            var boek = await _db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (boek == null) return NotFound();
            return Ok(boek);
        }

        // POST: api/Boeken
        [HttpPost]
        [Authorize(Policy = "RequireStaff")]
        public async Task<ActionResult<Boek>> Post(Boek model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
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
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, saved);
        }

        // PUT: api/Boeken/5
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Put(int id, Boek model)
        {
            if (id != model.Id) return BadRequest();
            var existing = await _db.Boeken.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

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
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            _db.Boeken.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
