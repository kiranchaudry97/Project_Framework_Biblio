using System.Collections.Generic;
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
    [Route("api/leden")]
    [Authorize(Policy = "RequireStaff")]
    public class LedenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        private readonly IMapper _mapper;
        public LedenApiController(BiblioDbContext db, IMapper mapper) => (_db, _mapper) = (db, mapper);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LidDto>>> Get()
        {
            var list = await _db.Leden.Where(l => !l.IsDeleted).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<LidDto>>(list));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LidDto>> Get(int id)
        {
            var item = await _db.Leden.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (item == null) return NotFound();
            return Ok(_mapper.Map<LidDto>(item));
        }

        [HttpPost]
        public async Task<ActionResult<LidDto>> Post(LidDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var entity = _mapper.Map<Lid>(model);
            _db.Leden.Add(entity);
            await _db.SaveChangesAsync();
            var dto = _mapper.Map<LidDto>(entity);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, LidDto model)
        {
            if (id != model.Id) return BadRequest();
            var existing = await _db.Leden.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            _mapper.Map(model, existing);

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
