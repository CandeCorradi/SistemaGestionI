using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.DataContext;
using Service.Models;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MayoristasController : ControllerBase
    {
        private readonly SistemaGestionContext _context;

        public MayoristasController(SistemaGestionContext context)
        {
            _context = context;
        }

        // GET: api/Mayoristas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mayorista>>> GetMayoristas()
        {
            return await _context.Mayoristas.ToListAsync();
        }

        // GET: api/Mayoristas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Mayorista>> GetMayorista(int id)
        {
            var mayorista = await _context.Mayoristas.FindAsync(id);

            if (mayorista == null)
            {
                return NotFound();
            }

            return mayorista;
        }

        // PUT: api/Mayoristas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMayorista(int id, Mayorista mayorista)
        {
            if (id != mayorista.Id)
            {
                return BadRequest();
            }

            _context.Entry(mayorista).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MayoristaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Mayoristas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Mayorista>> PostMayorista(Mayorista mayorista)
        {
            _context.Mayoristas.Add(mayorista);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMayorista", new { id = mayorista.Id }, mayorista);
        }

        // DELETE: api/Mayoristas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMayorista(int id)
        {
            var mayorista = await _context.Mayoristas.FindAsync(id);
            if (mayorista == null)
            {
                return NotFound();
            }
            mayorista.IsDeleted = true; //soft delete
            _context.Mayoristas.Update(mayorista);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MayoristaExists(int id)
        {
            return _context.Mayoristas.Any(e => e.Id == id);
        }
    }
}
