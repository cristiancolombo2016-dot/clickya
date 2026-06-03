using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicacionesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public PublicacionesController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        [HttpGet("todas")]
        public async Task<IActionResult> GetTodas()
        {
            var lista = await _db.PublicacionesComercios
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("comercio/{comercioId}")]
        public async Task<IActionResult> GetPorComercio(int comercioId)
        {
            var lista = await _db.PublicacionesComercios
                .Where(x => x.ComercioId == comercioId)
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Crear([FromForm] PublicacionComercioForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Titulo))
                return BadRequest("Falta título");

            var imagenesUrls = await GuardarImagenes(form.Imagenes, 10);

            var nueva = new PublicacionComercio
            {
                ComercioId = form.ComercioId,
                Titulo = form.Titulo,
                Descripcion = form.Descripcion ?? "",
                Precio = form.Precio ?? "",
                Rubro = form.Rubro ?? "",
                ImagenUrl = imagenesUrls.FirstOrDefault() ?? "",
                ImagenesUrls = imagenesUrls,
                DatosExtraJson = form.DatosExtraJson ?? "{}",
                FechaCreacion = DateTime.UtcNow
            };

            _db.PublicacionesComercios.Add(nueva);
            await _db.SaveChangesAsync();
            return Ok(nueva);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Editar(int id, [FromForm] PublicacionComercioForm form)
        {
            var existente = await _db.PublicacionesComercios.FindAsync(id);
            if (existente == null) return NotFound("Publicación no encontrada");

            existente.Titulo = form.Titulo ?? existente.Titulo;
            existente.Descripcion = form.Descripcion ?? existente.Descripcion;
            existente.Precio = form.Precio ?? existente.Precio;

            if (form.Imagenes != null && form.Imagenes.Count > 0)
            {
                var nuevasImagenes = await GuardarImagenes(form.Imagenes, 10);
                existente.ImagenesUrls = nuevasImagenes;
                existente.ImagenUrl = nuevasImagenes.FirstOrDefault() ?? existente.ImagenUrl;
            }

            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var existente = await _db.PublicacionesComercios.FindAsync(id);
            if (existente == null) return NotFound("Publicación no encontrada");
            _db.PublicacionesComercios.Remove(existente);
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

    public class PublicacionComercioForm
    {
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? Precio { get; set; }
        public string? Rubro { get; set; }
        public int ComercioId { get; set; }
        public string? DatosExtraJson { get; set; }
        public List<IFormFile>? Imagenes { get; set; }
    }
}