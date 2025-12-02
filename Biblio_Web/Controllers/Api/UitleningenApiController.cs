/*
API endpoints (UitleningenApiController)
- GET  /api/uitleningen?page=1&pageSize=20   -> paged list (RequireMember)
- GET  /api/uitleningen/late                -> list overdue loans (RequireMember)
- POST /api/uitleningen                      -> create loan (RequireStaff)
- PUT  /api/uitleningen/{id}/return          -> mark loan returned (RequireStaff)
*/

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

        // GET: api/uitleningen?page=1&pageSize=20
        // GET  /api/uitleningen?page=1&pageSize=20   -> paged list (RequireMember)
        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var q = _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => !l.IsDeleted).AsQueryable();
            var total = await q.CountAsync();
            var totalPages = (int)System.Math.Ceiling(total / (double)pageSize);
            var items = await q.OrderByDescending(l => l.StartDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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

        // GET: api/uitleningen/late
        // GET  /api/uitleningen/late                -> list overdue loans (RequireMember)
        [HttpGet("late")]
        public async Task<ActionResult<IEnumerable<Lenen>>> GetLate()
        {
            var today = System.DateTime.Today;
            var list = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => l.DueDate < today && l.ReturnedAt == null && !l.IsDeleted).ToListAsync();
            return Ok(list);
        }

        // POST: api/uitleningen
        // POST /api/uitleningen                      -> create loan (RequireStaff)
        [HttpPost]
        [Authorize(Policy = "RequireStaff")]
        public async Task<ActionResult<Lenen>> Post(Lenen model)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            var exists = await _db.Leningens.AnyAsync(l => l.BoekId == model.BoekId && l.ReturnedAt == null && !l.IsDeleted);
            if (exists) return Conflict(new ProblemDetails { Title = "Conflict", Detail = "Book already loaned" });

            var entity = new Lenen
            {
                BoekId = model.BoekId,
                LidId = model.LidId,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                ReturnedAt = model.ReturnedAt,
                IsClosed = model.IsClosed
            };

            _db.Leningens.Add(entity);
            await _db.SaveChangesAsync();

            var saved = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).FirstOrDefaultAsync(l => l.Id == entity.Id);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, saved);
        }

        // PUT: api/uitleningen/{id}/return
        // PUT  /api/uitleningen/{id}/return          -> mark loan returned (RequireStaff)
        [HttpPut("{id}/return")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Return(int id)
        {
            var loan = await _db.Leningens.FindAsync(id);
            if (loan == null || loan.IsDeleted) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Loan not found" });
            loan.ReturnedAt = System.DateTime.Now;
            loan.IsClosed = true;
            _db.Leningens.Update(loan);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
