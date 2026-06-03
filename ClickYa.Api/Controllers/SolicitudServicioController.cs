using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolicitudServicioController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public SolicitudServicioController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.SolicitudesServicio
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("rubro/{rubro}")]
        public async Task<IActionResult> GetPorRubro(string rubro)
        {
            var lista = await _db.SolicitudesServicio
                .Where(s => s.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Crear(
            [FromForm] string rubro,
            [FromForm] string descripcion,
            [FromForm] string whatsAppCliente,
            IFormFile? imagen)
        {
            var imagenUrl = "";
            if (imagen != null && imagen.Length > 0)
            {
                if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
                var ext = Path.GetExtension(imagen.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(_uploadsPath, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await imagen.CopyToAsync(stream);
                imagenUrl = $"/uploads/{fileName}";
            }

            var nueva = new SolicitudServicio
            {
                Rubro = rubro,
                Descripcion = descripcion,
                WhatsAppCliente = whatsAppCliente,
                ImagenUrl = imagenUrl,
                Fecha = DateTime.UtcNow,
                Estado = "Pendiente"
            };

            _db.SolicitudesServicio.Add(nueva);
            await _db.SaveChangesAsync();
            return Ok(nueva);
        }

        [HttpPut("{id}/estado")]
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