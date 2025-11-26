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
    [Route("api/categorieen")]
    [Authorize(Policy = "RequireStaff")]
    public class CategorieenApiController : ControllerBase
    {
        private readonly BiblioDbContext _db;
        private readonly IMapper _mapper;
        public CategorieenApiController(BiblioDbContext db, IMapper mapper) => (_db, _mapper) = (db, mapper);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategorieDto>>> Get()
        {
            var list = await _db.Categorien.Where(c => !c.IsDeleted).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<CategorieDto>>(list));
        }

        [HttpPost]
        public async Task<ActionResult<CategorieDto>> Post(CategorieDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var entity = _mapper.Map<Categorie>(model);
            _db.Categorien.Add(entity);
            await _db.SaveChangesAsync();
            var dto = _mapper.Map<CategorieDto>(entity);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, CategorieDto model)
        {
            if (id != model.Id) return BadRequest();
            var existing = await _db.Categorien.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _mapper.Map(model, existing);
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
