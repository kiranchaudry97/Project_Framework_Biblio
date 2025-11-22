using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using AutoMapper;
using Biblio_Web.ApiModels;

namespace Biblio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/uitleningen")]
    [Authorize(Policy = "RequireMember")]
    public class UitleningenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        private readonly IMapper _mapper;
        public UitleningenApiController(BiblioDbContext db, IMapper mapper) => (_db, _mapper) = (db, mapper);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UitleningDto>>> Get()
        {
            var list = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => !l.IsDeleted).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<UitleningDto>>(list));
        }

        [HttpGet("late")]
        public async Task<ActionResult<IEnumerable<UitleningDto>>> GetLate()
        {
            var today = System.DateTime.Today;
            var list = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).Where(l => l.DueDate < today && l.ReturnedAt == null && !l.IsDeleted).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<UitleningDto>>(list));
        }

        [HttpPost]
        [Authorize(Policy = "RequireStaff")]
        public async Task<ActionResult<UitleningDto>> Post(UitleningDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var exists = await _db.Leningens.AnyAsync(l => l.BoekId == model.BoekId && l.ReturnedAt == null && !l.IsDeleted);
            if (exists) return Conflict("Boek is al uitgeleend.");
            var entity = _mapper.Map<Lenen>(model);
            _db.Leningens.Add(entity);
            await _db.SaveChangesAsync();
            var saved = await _db.Leningens.Include(l => l.Boek).Include(l => l.Lid).FirstOrDefaultAsync(l => l.Id == entity.Id);
            var dto = _mapper.Map<UitleningDto>(saved!);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
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
