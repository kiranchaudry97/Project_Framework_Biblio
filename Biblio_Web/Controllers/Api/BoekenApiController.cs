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
    [Route("api/boeken")]
    [Authorize(Policy = "RequireMember")]
    public class BoekenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        private readonly IMapper _mapper;
        public BoekenApiController(BiblioDbContext db, IMapper mapper) => (_db, _mapper) = (db, mapper);

        // GET: api/Boeken
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoekDto>>> Get()
        {
            var list = await _db.Boeken.Where(b => !b.IsDeleted).Include(b => b.categorie).ToListAsync();
            var dto = _mapper.Map<IEnumerable<BoekDto>>(list);
            return Ok(dto);
        }

        // GET: api/Boeken/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BoekDto>> Get(int id)
        {
            var boek = await _db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (boek == null) return NotFound();
            return Ok(_mapper.Map<BoekDto>(boek));
        }

        // POST: api/Boeken
        [HttpPost]
        [Authorize(Policy = "RequireStaff")]
        public async Task<ActionResult<BoekDto>> Post(BoekDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var entity = _mapper.Map<Boek>(model);
            _db.Boeken.Add(entity);
            await _db.SaveChangesAsync();

            var saved = await _db.Boeken.Include(b => b.categorie).FirstOrDefaultAsync(b => b.Id == entity.Id);
            var dto = _mapper.Map<BoekDto>(saved!);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        // PUT: api/Boeken/5
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> Put(int id, BoekDto model)
        {
            if (id != model.Id) return BadRequest();
            var existing = await _db.Boeken.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            // map incoming DTO onto existing entity (Categorie navigation is ignored by mapping)
            _mapper.Map(model, existing);

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
