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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categorie>>> Get()
        {
            var list = await _db.Categorien.Where(c => !c.IsDeleted).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<Categorie>> Post(Categorie model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var entity = new Categorie { Naam = model.Naam };
            _db.Categorien.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Categorie model)
        {
            if (id != model.Id) return BadRequest();
            var existing = await _db.Categorien.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.Naam = model.Naam;
            _db.Categorien.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Categorien.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            _db.Categorien.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
