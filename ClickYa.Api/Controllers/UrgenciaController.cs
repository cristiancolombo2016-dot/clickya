using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ClickYa.Api.Security;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrgenciaController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public UrgenciaController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        [HttpPost("foto")]
        [AllowAnonymous]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubirFoto(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Sin archivo");
            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(_uploadsPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);
            return Ok(new { url = $"/uploads/{fileName}" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Crear([FromBody] SolicitudUrgencia solicitud)
        {
            solicitud.Fecha = DateTime.UtcNow;
            solicitud.Estado = "Nueva";
            _db.Urgencias.Add(solicitud);
            await _db.SaveChangesAsync();
            return Ok(solicitud);
        }

        [HttpGet("tecnico/{tecnicoId}")]
        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        public async Task<IActionResult> GetPorTecnico(int tecnicoId)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, tecnicoId))
                return Forbid();
            var lista = await _db.Urgencias
                .Where(x => x.TecnicoId == tecnicoId)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPut("{id}/estado")]
        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] string estado)
        {
            var item = await _db.Urgencias.FindAsync(id);
            if (item == null) return NotFound();
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, item.TecnicoId))
                return Forbid();
            item.Estado = estado;
            await _db.SaveChangesAsync();
            return Ok(item);
        }
    }
}
