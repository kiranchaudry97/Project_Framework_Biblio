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
    [Route("api/uitleningen")]
    [Authorize(Policy = "RequireMember")]
    public class UitleningenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        public UitleningenApiController(BiblioDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lenen>>> Get()
        {
            var list = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => !l.IsDeleted).ToListAsync();
            return Ok(list);
        }

        [HttpGet("late")]
        public async Task<ActionResult<IEnumerable<Lenen>>> GetLate()
        {
            var today = System.DateTime.Today;
            var list = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => l.DueDate < today && l.ReturnedAt == null && !l.IsDeleted).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Policy = "RequireStaff")]
        public async Task<ActionResult<Lenen>> Post(Lenen model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var exists = await _db.Leningens.AnyAsync(l => l.BoekId == model.BoekId && l.ReturnedAt == null && !l.IsDeleted);
            if (exists) return Conflict("Boek is al uitgeleend.");
            var entity = new Lenen
            {
                BoekId = model.BoekId,
                LidId = model.LidId,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                IsClosed = false
            };
            _db.Leningens.Add(entity);
            await _db.SaveChangesAsync();
            var saved = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).FirstOrDefaultAsync(l => l.Id == entity.Id);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, saved);
        }

        [HttpPut("{id}/return")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Return(int id)
        {
            var loan = await _db.Leningens.FindAsync(id);
            if (loan == null || loan.IsDeleted) return NotFound();
            loan.ReturnedAt = System.DateTime.Now;
            loan.IsClosed = true;
            _db.Leningens.Update(loan);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
