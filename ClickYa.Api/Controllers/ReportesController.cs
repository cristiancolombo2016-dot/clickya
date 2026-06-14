using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReportesController(AppDbContext db)
        {
            _db = db;
        }

        // Listar todos los reportes (para el panel admin)
        [HttpGet]
        public async Task<IActionResult> GetTodos()
        {
            var lista = await _db.Reportes
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(lista);
        }

        // Crear un reporte (desde la app)
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Reporte reporte)
        {
            reporte.CreatedAt = DateTime.UtcNow;
            reporte.Estado = "PENDIENTE";
            _db.Reportes.Add(reporte);
            await _db.SaveChangesAsync();
            return Ok(reporte);
        }

        // Eliminar un reporte (cuando ya lo resolviste)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var reporte = await _db.Reportes.FindAsync(id);
            if (reporte == null) return NotFound();
            _db.Reportes.Remove(reporte);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}