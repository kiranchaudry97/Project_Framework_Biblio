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

        // GET: api/leden?page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var q = _db.Leden.Where(l => !l.IsDeleted).AsQueryable();
            var total = await q.CountAsync();
            var totalPages = (int)System.Math.Ceiling(total / (double)pageSize);
            var items = await q.OrderBy(l => l.Voornaam).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var result = new
            {
                page,
                pageSize,
                total,
                totalPages,
                items
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _db.Leden.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (item == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Member not found" });
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post(Lid model)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
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
            if (id != model.Id) return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Id mismatch" });
            var existing = await _db.Leden.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Member not found" });

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
            if (existing == null || existing.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Member not found" });
            existing.IsDeleted = true;
            _db.Leden.Update(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
