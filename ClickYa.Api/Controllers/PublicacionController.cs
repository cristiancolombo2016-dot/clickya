using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicacionController : ControllerBase
    {
        private static readonly object _lock = new();
        private string DataFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private string DataFile => Path.Combine(DataFolder, "publicaciones.json");
        private string TecnicosFile => Path.Combine(DataFolder, "tecnicos.json");
        private string UploadsFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(Leer().OrderByDescending(x => x.FechaCreacion));
        }

        [HttpGet("tecnico/{tecnicoId}")]
        public IActionResult GetPorTecnico(int tecnicoId)
        {
            var lista = Leer()
                .Where(x => x.TecnicoId == tecnicoId)
                .OrderByDescending(x => x.FechaCreacion)
                .ToList();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var pub = Leer().FirstOrDefault(x => x.Id == id);
            if (pub == null) return NotFound();
            return Ok(pub);
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Crear([FromQuery] string token, [FromForm] PublicacionForm form)
        {
            var tecnico = ObtenerTecnicoPorToken(token);
            if (tecnico == null) return Unauthorized("Token inválido");
            if (string.IsNullOrWhiteSpace(form.Titulo)) return BadRequest("Falta título");

            int maxImagenes = tecnico.EsPremium ? 20 : 8;
            var imagenesUrls = await GuardarImagenes(form.Imagenes, maxImagenes);

            var lista = Leer();
            var nueva = new Publicacion
            {
                Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1,
                TecnicoId = tecnico.Id,
                Titulo = form.Titulo,
                Descripcion = form.Descripcion ?? "",
                Imagenes = imagenesUrls,
                FechaCreacion = DateTime.Now
            };

            lista.Add(nueva);
            Guardar(lista);
            return Ok(nueva);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Editar(int id, [FromQuery] string token, [FromForm] PublicacionForm form)
        {
            var tecnico = ObtenerTecnicoPorToken(token);
            if (tecnico == null) return Unauthorized("Token inválido");

            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id && x.TecnicoId == tecnico.Id);
            if (existente == null) return NotFound();

            existente.Titulo = form.Titulo ?? existente.Titulo;
            existente.Descripcion = form.Descripcion ?? existente.Descripcion;

            if (form.Imagenes != null && form.Imagenes.Count > 0)
            {
                int maxImagenes = tecnico.EsPremium ? 20 : 8;
                existente.Imagenes = await GuardarImagenes(form.Imagenes, maxImagenes);
            }

            Guardar(lista);
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id, [FromQuery] string token)
        {
            var tecnico = ObtenerTecnicoPorToken(token);
            if (tecnico == null) return Unauthorized("Token inválido");

            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id && x.TecnicoId == tecnico.Id);
            if (existente == null) return NotFound();

            lista.Remove(existente);
            Guardar(lista);
            return Ok();
        }

        private Tecnico? ObtenerTecnicoPorToken(string token)
        {
            if (!System.IO.File.Exists(TecnicosFile)) return null;
            var tecnicos = JsonSerializer.Deserialize<List<Tecnico>>(
                System.IO.File.ReadAllText(TecnicosFile), _opts) ?? new();
            return tecnicos.FirstOrDefault(t => t.Token == token && t.Activo);
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

        private List<Publicacion> Leer()
        {
            lock (_lock)
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
                if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
                return JsonSerializer.Deserialize<List<Publicacion>>(
                    System.IO.File.ReadAllText(DataFile), _opts) ?? new();
            }
        }

        private void Guardar(List<Publicacion> lista)
        {
            lock (_lock)
            {
                System.IO.File.WriteAllText(DataFile,
                    JsonSerializer.Serialize(lista, _opts));
            }
        }
    }

    public class PublicacionForm
    {
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public List<IFormFile>? Imagenes { get; set; }
    }
}