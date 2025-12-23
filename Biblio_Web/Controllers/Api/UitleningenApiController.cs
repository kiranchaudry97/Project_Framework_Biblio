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

            var dtoItems = items.Select(l => new Biblio_Web.Models.ApiDtos.LenenDto
            {
                Id = l.Id,
                BoekId = l.BoekId,
                LidId = l.LidId,
                StartDate = l.StartDate,
                DueDate = l.DueDate,
                ReturnedAt = l.ReturnedAt,
                IsClosed = l.IsClosed,
                Boek = l.Boek == null ? null : new Biblio_Web.Models.ApiDtos.BoekDto
                {
                    Id = l.Boek.Id,
                    Titel = l.Boek.Titel,
                    Auteur = l.Boek.Auteur,
                    Isbn = l.Boek.Isbn,
                    CategorieID = l.Boek.CategorieID,
                    CategorieNaam = l.Boek.categorie is null ? string.Empty : l.Boek.categorie.Naam
                },
                Lid = l.Lid == null ? null : new Biblio_Web.Models.ApiDtos.LidDto
                {
                    Id = l.Lid.Id,
                    Voornaam = l.Lid.Voornaam,
                    AchterNaam = l.Lid.AchterNaam,
                    Email = l.Lid.Email
                }
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

        // GET: api/uitleningen/late
        // GET  /api/uitleningen/late                -> list overdue loans (RequireMember)
        [HttpGet("late")]
        public async Task<ActionResult<IEnumerable<Biblio_Web.Models.ApiDtos.LenenDto>>> GetLate()
        {
            var today = System.DateTime.Today;
            var list = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => l.DueDate < today && l.ReturnedAt == null && !l.IsDeleted).ToListAsync();

            var dto = list.Select(l => new Biblio_Web.Models.ApiDtos.LenenDto
            {
                Id = l.Id,
                BoekId = l.BoekId,
                LidId = l.LidId,
                StartDate = l.StartDate,
                DueDate = l.DueDate,
                ReturnedAt = l.ReturnedAt,
                IsClosed = l.IsClosed,
                Boek = l.Boek == null ? null : new Biblio_Web.Models.ApiDtos.BoekDto
                {
                    Id = l.Boek.Id,
                    Titel = l.Boek.Titel,
                    Auteur = l.Boek.Auteur,
                    Isbn = l.Boek.Isbn,
                    CategorieID = l.Boek.CategorieID,
                    CategorieNaam = l.Boek.categorie is null ? string.Empty : l.Boek.categorie.Naam
                },
                Lid = l.Lid == null ? null : new Biblio_Web.Models.ApiDtos.LidDto
                {
                    Id = l.Lid.Id,
                    Voornaam = l.Lid.Voornaam,
                    AchterNaam = l.Lid.AchterNaam,
                    Email = l.Lid.Email
                }
            }).ToList();

            return Ok(dto);
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

            var savedDto = new Biblio_Web.Models.ApiDtos.LenenDto
            {
                Id = saved.Id,
                BoekId = saved.BoekId,
                LidId = saved.LidId,
                StartDate = saved.StartDate,
                DueDate = saved.DueDate,
                ReturnedAt = saved.ReturnedAt,
                IsClosed = saved.IsClosed,
                Boek = saved.Boek == null ? null : new Biblio_Web.Models.ApiDtos.BoekDto
                {
                    Id = saved.Boek.Id,
                    Titel = saved.Boek.Titel,
                    Auteur = saved.Boek.Auteur,
                    Isbn = saved.Boek.Isbn,
                    CategorieID = saved.Boek.CategorieID,
                    CategorieNaam = saved.Boek.categorie is null ? string.Empty : saved.Boek.categorie.Naam
                },
                Lid = saved.Lid == null ? null : new Biblio_Web.Models.ApiDtos.LidDto
                {
                    Id = saved.Lid.Id,
                    Voornaam = saved.Lid.Voornaam,
                    AchterNaam = saved.Lid.AchterNaam,
                    Email = saved.Lid.Email
                }
            };

            return CreatedAtAction(nameof(Get), new { id = entity.Id }, savedDto);
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
