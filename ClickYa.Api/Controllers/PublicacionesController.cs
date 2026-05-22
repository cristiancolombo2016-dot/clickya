using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicacionesController : ControllerBase
    {
        private static readonly object _lock = new();
        private string DataFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private string DataFile => Path.Combine(DataFolder, "publicaciones_comercio.json");
        private string UploadsFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        [HttpGet("todas")]
        public IActionResult GetTodas()
        {
            var lista = Leer().OrderByDescending(x => x.FechaCreacion).ToList();
            return Ok(lista);
        }

        [HttpGet("comercio/{comercioId}")]
        public IActionResult GetPorComercio(int comercioId)
        {
            var lista = Leer()
                .Where(x => x.ComercioId == comercioId)
                .OrderByDescending(x => x.FechaCreacion)
                .ToList();
            return Ok(lista);
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Crear([FromForm] PublicacionComercioForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Titulo))
                return BadRequest("Falta título");

            var imagenesUrls = await GuardarImagenes(form.Imagenes, 10);
            var imagenPrincipal = imagenesUrls.FirstOrDefault() ?? "";

            var lista = Leer();
            var nueva = new PublicacionComercio
            {
                Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1,
                ComercioId = form.ComercioId,
                Titulo = form.Titulo,
                Descripcion = form.Descripcion ?? "",
                Precio = form.Precio ?? "",
                Rubro = form.Rubro ?? "",
                ImagenUrl = imagenPrincipal,
                ImagenesUrls = imagenesUrls,
                DatosExtraJson = form.DatosExtraJson ?? "{}",
                FechaCreacion = DateTime.Now
            };

            lista.Add(nueva);
            Guardar(lista);
            return Ok(nueva);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Editar(int id, [FromForm] PublicacionComercioForm form)
        {
            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id);
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

            Guardar(lista);
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id)
        {
            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id);
            if (existente == null) return NotFound("Publicación no encontrada");

            lista.Remove(existente);
            Guardar(lista);
            return Ok();
        }

        private async Task<List<string>> GuardarImagenes(List<IFormFile>? imagenes, int max)
        {
            var urls = new List<string>();
            if (imagenes == null || imagenes.Count == 0) return urls;
            if (!Directory.Exists(UploadsFolder)) Directory.CreateDirectory(UploadsFolder);

            foreach (var imagen in imagenes.Take(max))
            {
                if (imagen.Length == 0) continue;
                var fileName = Guid.NewGuid() + Path.GetExtension(imagen.FileName);
                var filePath = Path.Combine(UploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await imagen.CopyToAsync(stream);
                urls.Add($"/uploads/{fileName}");
            }
            return urls;
        }

        private List<PublicacionComercio> Leer()
        {
            lock (_lock)
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
                if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
                return JsonSerializer.Deserialize<List<PublicacionComercio>>(
                    System.IO.File.ReadAllText(DataFile), _opts) ?? new();
            }
        }

        private void Guardar(List<PublicacionComercio> lista)
        {
            lock (_lock)
            {
                System.IO.File.WriteAllText(DataFile,
                    JsonSerializer.Serialize(lista, _opts));
            }
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