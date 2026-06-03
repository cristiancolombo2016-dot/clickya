using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TecnicoController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public TecnicoController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.Tecnicos.ToListAsync();
            return Ok(lista);
        }

        [HttpGet("rubro/{rubro}")]
        public async Task<IActionResult> GetPorRubro(string rubro)
        {
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("categoria/{rubro}")]
        public async Task<IActionResult> GetPorCategoria(string rubro)
        {
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(t => t.EsPremium)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("urgencias")]
        public async Task<IActionResult> GetUrgencias()
        {
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.EsPremium)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tecnico = await _db.Tecnicos.FindAsync(id);
            if (tecnico == null) return NotFound();
            return Ok(tecnico);
        }

        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetPorToken(string token)
        {
            var tecnico = await _db.Tecnicos.FirstOrDefaultAsync(t => t.Token == token);
            if (tecnico == null) return NotFound();
            return Ok(tecnico);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Tecnico tecnico)
        {
            _db.Tecnicos.Add(tecnico);
            await _db.SaveChangesAsync();
            return Ok(tecnico);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] Tecnico tecnico)
        {
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nombre = tecnico.Nombre;
            existente.Rubro = tecnico.Rubro;
            existente.WhatsApp = tecnico.WhatsApp;
            existente.Activo = tecnico.Activo;
            existente.EsPremium = tecnico.EsPremium;
            existente.FechaPremium = tecnico.FechaPremium;
            existente.FotoPortada = tecnico.FotoPortada;
            existente.Logo = tecnico.Logo;
            existente.Ubicacion = tecnico.Ubicacion;
            existente.Direccion = tecnico.Direccion;
            existente.Descripcion = tecnico.Descripcion;
            existente.Instagram = tecnico.Instagram;
            existente.Latitud = tecnico.Latitud;
            existente.Longitud = tecnico.Longitud;
            if (!string.IsNullOrWhiteSpace(tecnico.Token))
                existente.Token = tecnico.Token;

            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            _db.Tecnicos.Remove(existente);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{id}/portada")]
        public async Task<IActionResult> SubirPortada(int id, IFormFile archivo)
        {
            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(_uploadsPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            existente.FotoPortada = $"/uploads/{fileName}";
            await _db.SaveChangesAsync();
            return Ok(new { portadaUrl = existente.FotoPortada });
        }

        [HttpPost("{id}/logo")]
        public async Task<IActionResult> SubirLogo(int id, IFormFile archivo)
        {
            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(_uploadsPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            existente.Logo = $"/uploads/{fileName}";
            await _db.SaveChangesAsync();
            return Ok(new { logoUrl = existente.Logo });
        }
    }
}