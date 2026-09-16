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
        private readonly SafeImageStorage _images;

        public PublicacionController(AppDbContext db, SafeImageStorage images)
        {
            _db = db;
            _images = images;
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

            int maxImagenes = PremiumMembership.IsVigente(tecnico, DateTime.UtcNow) ? 20 : 8;
            var imagenesUrls = await _images.SaveManyAsync(form.Imagenes, maxImagenes);
            if (imagenesUrls == null)
                return BadRequest($"Podés subir hasta {maxImagenes} imágenes JPG, PNG o WEBP de 10 MB cada una.");

            var nueva = new Publicacion
            {
                TecnicoId = tecnico.Id,
                Titulo = form.Titulo,
                Descripcion = form.Descripcion ?? "",
                Imagenes = imagenesUrls,
                FechaCreacion = DateTime.UtcNow
            };

            _db.Publicaciones.Add(nueva);
            try { await _db.SaveChangesAsync(); }
            catch { imagenesUrls.ForEach(_images.Delete); throw; }
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
                int maxImagenes = PremiumMembership.IsVigente(tecnico, DateTime.UtcNow) ? 20 : 8;
                var nuevasImagenes = await _images.SaveManyAsync(form.Imagenes, maxImagenes);
                if (nuevasImagenes == null)
                    return BadRequest($"Podés subir hasta {maxImagenes} imágenes JPG, PNG o WEBP de 10 MB cada una.");
                existente.Imagenes = nuevasImagenes;
                try { await _db.SaveChangesAsync(); }
                catch { nuevasImagenes.ForEach(_images.Delete); throw; }
                return Ok(existente);
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

    }

    public class PublicacionForm
    {
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public List<IFormFile>? Imagenes { get; set; }
    }
}
