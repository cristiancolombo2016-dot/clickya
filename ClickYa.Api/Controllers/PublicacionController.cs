using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicacionController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public PublicacionController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.Publicaciones
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("tecnico/{tecnicoId}")]
        public async Task<IActionResult> GetPorTecnico(int tecnicoId)
        {
            var lista = await _db.Publicaciones
                .Where(x => x.TecnicoId == tecnicoId)
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pub = await _db.Publicaciones.FindAsync(id);
            if (pub == null) return NotFound();
            return Ok(pub);
        }

        [Authorize(Roles = SecurityDefaults.TecnicoRole)]
        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Crear([FromForm] PublicacionForm form)
        {
            var tecnicoId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var tecnico = await _db.Tecnicos.FirstOrDefaultAsync(t => t.Id == tecnicoId && t.Activo);
            if (tecnico == null) return Unauthorized("Token inválido");
            if (string.IsNullOrWhiteSpace(form.Titulo)) return BadRequest("Falta título");

            int maxImagenes = tecnico.EsPremium ? 20 : 8;
            var imagenesUrls = await GuardarImagenes(form.Imagenes, maxImagenes);

            var nueva = new Publicacion
            {
                TecnicoId = tecnico.Id,
                Titulo = form.Titulo,
                Descripcion = form.Descripcion ?? "",
                Imagenes = imagenesUrls,
                FechaCreacion = DateTime.UtcNow
            };

            _db.Publicaciones.Add(nueva);
            await _db.SaveChangesAsync();
            return Ok(nueva);
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPut("{id}")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Editar(int id, [FromForm] PublicacionForm form)
        {
            var existente = await _db.Publicaciones.FirstOrDefaultAsync(x => x.Id == id);
            if (existente == null) return NotFound();
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, existente.TecnicoId)) return Forbid();

            var tecnico = await _db.Tecnicos.FindAsync(existente.TecnicoId);
            if (tecnico == null || !tecnico.Activo) return Unauthorized();

            existente.Titulo = form.Titulo ?? existente.Titulo;
            existente.Descripcion = form.Descripcion ?? existente.Descripcion;

            if (form.Imagenes != null && form.Imagenes.Count > 0)
            {
                int maxImagenes = tecnico.EsPremium ? 20 : 8;
                existente.Imagenes = await GuardarImagenes(form.Imagenes, maxImagenes);
            }

            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var existente = await _db.Publicaciones.FirstOrDefaultAsync(x => x.Id == id);
            if (existente == null) return NotFound();
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, existente.TecnicoId)) return Forbid();

            _db.Publicaciones.Remove(existente);
            await _db.SaveChangesAsync();
            return Ok();
        }

        private async Task<List<string>> GuardarImagenes(List<IFormFile>? imagenes, int max)
        {
            var urls = new List<string>();
            if (imagenes == null || imagenes.Count == 0) return urls;
            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);

            foreach (var imagen in imagenes.Take(max))
            {
                if (imagen.Length == 0) continue;
                var fileName = Guid.NewGuid() + Path.GetExtension(imagen.FileName);
                var filePath = Path.Combine(_uploadsPath, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await imagen.CopyToAsync(stream);
                urls.Add($"/uploads/{fileName}");
            }
            return urls;
        }
    }

    public class PublicacionForm
    {
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public List<IFormFile>? Imagenes { get; set; }
    }
}
