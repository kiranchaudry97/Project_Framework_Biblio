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
    [Route("api/leden")]
    [Authorize(Policy = "RequireStaff")]
    public class LedenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        public LedenApiController(BiblioDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lid>>> Get()
        {
            var list = await _db.Leden.Where(l => !l.IsDeleted).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Lid>> Get(int id)
        {
            var item = await _db.Leden.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Lid>> Post(Lid model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var entity = new Lid
            {
                Voornaam = model.Voornaam,
                AchterNaam = model.AchterNaam,
                Email = model.Email,
                Telefoon = model.Telefoon,
                Adres = model.Adres
            };
            _db.Leden.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Lid model)
        {
            if (id != model.Id) return BadRequest();
            var existing = await _db.Leden.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            existing.Voornaam = model.Voornaam;
            existing.AchterNaam = model.AchterNaam;
            existing.Email = model.Email;
            existing.Telefoon = model.Telefoon;
            existing.Adres = model.Adres;

            _db.Leden.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Leden.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            _db.Leden.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
