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
    public class DetallePedidosController : ControllerBase
    {
        private readonly SistemaGestionContext _context;

        public DetallePedidosController(SistemaGestionContext context)
        {
            _context = context;
        }

        // GET: api/DetallePedidos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DetallePedido>>> GetDetallesPedidos()
        {
            return await _context.DetallesPedidos.ToListAsync();
        }

        // GET: api/DetallePedidos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DetallePedido>> GetDetallePedido(int id)
        {
            var detallePedido = await _context.DetallesPedidos.FindAsync(id);

            if (detallePedido == null)
            {
                return NotFound();
            }

            return detallePedido;
        }

        // GET: api/DetallePedidos/deleteds/
        [HttpGet("deleteds/")]
        public async Task<ActionResult<IEnumerable<DetallePedido>>> GetDetallePedidosDeleteds()
        {
            return await _context.DetallesPedidos.IgnoreQueryFilters().Where(c => c.IsDeleted).ToListAsync();
        }

        // PUT: api/DetallePedidos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDetallePedido(int id, DetallePedido detallePedido)
        {
            if (id != detallePedido.Id)
            {
                return BadRequest();
            }

            _context.Entry(detallePedido).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DetallePedidoExists(id))
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

        // POST: api/DetallePedidos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DetallePedido>> PostDetallePedido(DetallePedido detallePedido)
        {
            _context.DetallesPedidos.Add(detallePedido);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDetallePedido", new { id = detallePedido.Id }, detallePedido);
        }

        // DELETE: api/DetallePedidos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDetallePedido(int id)
        {
            var detallePedido = await _context.DetallesPedidos.FindAsync(id);
            if (detallePedido == null)
            {
                return NotFound();
            }

            detallePedido.IsDeleted = true; //soft delete
            _context.DetallesPedidos.Update(detallePedido);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Usuarios/restore/5
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreDetallePedido(int id)
        {
            var detallePedido = await _context.DetallesPedidos.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id.Equals(id));
            if (detallePedido == null)
            {
                return NotFound();
            }
            detallePedido.IsDeleted = false; // Soft restore
            _context.DetallesPedidos.Update(detallePedido);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool DetallePedidoExists(int id)
        {
            return _context.DetallesPedidos.Any(e => e.Id == id);
        }
    }
}
