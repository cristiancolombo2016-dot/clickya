using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ClickYa.Api.Security;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Obsolete("Flujo histórico reemplazado por SolicitudUrgencia y ofertas.")]
    public class SolicitudServicioController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SolicitudServicioController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.SolicitudesServicio
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("rubro/{rubro}")]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> GetPorRubro(string rubro)
        {
            var lista = await _db.SolicitudesServicio
                .Where(s => s.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        [AllowAnonymous]
        [Obsolete("Usar POST /api/Urgencia.")]
        public IActionResult Crear(
            [FromForm] string rubro,
            [FromForm] string descripcion,
            [FromForm] string whatsAppCliente,
            IFormFile? imagen)
            => StatusCode(StatusCodes.Status410Gone,
                "Este flujo quedó obsoleto. Actualizá la aplicación para publicar una urgencia.");

        [HttpPut("{id}/estado")]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] string estado)
        {
            var solicitud = await _db.SolicitudesServicio.FindAsync(id);
            if (solicitud == null) return NotFound();
            solicitud.Estado = estado;
            await _db.SaveChangesAsync();
            return Ok(solicitud);
        }
    }
}
