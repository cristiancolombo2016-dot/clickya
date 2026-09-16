using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.Tecnicos.ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("rubro/{rubro}")]
        public async Task<IActionResult> GetPorRubro(string rubro)
        {
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())
                .ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("categoria/{rubro}")]
        public async Task<IActionResult> GetPorCategoria(string rubro)
        {
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(t => t.EsPremium)
                .ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("urgencias")]
        public async Task<IActionResult> GetUrgencias()
        {
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.EsPremium)
                .ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tecnico = await _db.Tecnicos.FindAsync(id);
            if (tecnico == null) return NotFound();
            return Ok(tecnico);
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Tecnico tecnico)
        {
            tecnico.Token = "";
            _db.Tecnicos.Add(tecnico);
            await _db.SaveChangesAsync();
            return Ok(tecnico);
        }

        [AllowAnonymous]
        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] TecnicoRegistroRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre) ||
                string.IsNullOrWhiteSpace(request.Rubro) ||
                string.IsNullOrWhiteSpace(request.WhatsApp))
                return BadRequest("Nombre, rubro y WhatsApp son obligatorios.");

            var tecnico = new Tecnico
            {
                Nombre = request.Nombre.Trim(),
                Rubro = request.Rubro.Trim(),
                WhatsApp = request.WhatsApp.Trim(),
                Token = "",
                Activo = true,
                EsPremium = false
            };

            _db.Tecnicos.Add(tecnico);
            await _db.SaveChangesAsync();
            var tokens = HttpContext.RequestServices.GetRequiredService<AccessTokenService>();
            var tickets = HttpContext.RequestServices.GetRequiredService<WebLoginTicketService>();
            var accessToken = tokens.Create(SecurityDefaults.TecnicoRole, tecnico.Id, tecnico.Nombre);
            return Ok(new { tecnico.Id, tecnico.Nombre, dashboardTicket = tickets.Issue(accessToken) });
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpPost("{id:int}/access-ticket")]
        public async Task<IActionResult> GenerarAcceso(int id)
        {
            var tecnico = await _db.Tecnicos.FindAsync(id);
            if (tecnico == null) return NotFound();

            var tokens = HttpContext.RequestServices.GetRequiredService<AccessTokenService>();
            var tickets = HttpContext.RequestServices.GetRequiredService<WebLoginTicketService>();
            var accessToken = tokens.Create(SecurityDefaults.TecnicoRole, tecnico.Id, tecnico.Nombre);
            return Ok(new { dashboardTicket = tickets.Issue(accessToken) });
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] Tecnico tecnico)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, id)) return Forbid();
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nombre = tecnico.Nombre;
            existente.Rubro = tecnico.Rubro;
            existente.WhatsApp = tecnico.WhatsApp;
            if (User.IsInRole(SecurityDefaults.AdminRole))
            {
                existente.Activo = tecnico.Activo;
                existente.EsPremium = tecnico.EsPremium;
                existente.FechaPremium = tecnico.FechaPremium;
            }
            existente.FotoPortada = tecnico.FotoPortada;
            existente.Logo = tecnico.Logo;
            existente.Ubicacion = tecnico.Ubicacion;
            existente.Direccion = tecnico.Direccion;
            existente.Descripcion = tecnico.Descripcion;
            existente.Instagram = tecnico.Instagram;
            existente.Latitud = tecnico.Latitud;
            existente.Longitud = tecnico.Longitud;
            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            _db.Tecnicos.Remove(existente);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPost("{id}/portada")]
        public async Task<IActionResult> SubirPortada(int id, IFormFile archivo)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, id)) return Forbid();
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

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPost("{id}/logo")]
        public async Task<IActionResult> SubirLogo(int id, IFormFile archivo)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, id)) return Forbid();
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

    public sealed class TecnicoRegistroRequest
    {
        public string Nombre { get; set; } = "";
        public string Rubro { get; set; } = "";
        public string WhatsApp { get; set; } = "";
    }
}
