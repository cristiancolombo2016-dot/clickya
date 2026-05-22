using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BannersController : ControllerBase
    {
        private static readonly object _lock = new();

        private string DataFolder => Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "data");

        private string DataFile => Path.Combine(DataFolder, "banners.json");

        private string UploadsFolder => Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        [HttpGet]
        public IActionResult GetAll()
        {
            var lista = Leer();
            var activos = lista.Where(b => b.Activo &&
                b.Inicio.AddDays(b.Dias) >= DateTime.Now).ToList();
            return Ok(activos);
        }

        [HttpGet("seccion/{seccion}")]
        public IActionResult GetPorSeccion(string seccion)
        {
            var lista = Leer();
            var filtrados = lista
                .Where(b => b.Activo &&
                       b.Seccion.ToLower() == seccion.ToLower() &&
                       b.Inicio.AddDays(b.Dias) >= DateTime.Now)
                .ToList();
            return Ok(filtrados);
        }

        [HttpGet("todos")]
        public IActionResult GetTodos()
        {
            return Ok(Leer());
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Crear([FromForm] BannerForm form)
        {
            if (form.Imagen == null || form.Imagen.Length == 0)
                return BadRequest("Falta imagen");

            if (!Directory.Exists(UploadsFolder))
                Directory.CreateDirectory(UploadsFolder);

            var ext = Path.GetExtension(form.Imagen.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(UploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await form.Imagen.CopyToAsync(stream);

            var lista = Leer();

            var nuevo = new Banner
            {
                Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1,
                Seccion = form.Seccion ?? "home",
                ImagenUrl = $"/uploads/{fileName}",
                LocalId = form.LocalId,
                TecnicoId = form.TecnicoId,
                Dias = form.Dias > 0 ? form.Dias : 7,
                Inicio = DateTime.Now,
                Activo = true
            };

            lista.Add(nuevo);
            Guardar(lista);

            return Ok(nuevo);
        }

        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id)
        {
            var lista = Leer();
            var banner = lista.FirstOrDefault(x => x.Id == id);
            if (banner == null) return NotFound();

            lista.Remove(banner);
            Guardar(lista);

            return Ok();
        }

        private List<Banner> Leer()
        {
            lock (_lock)
            {
                if (!Directory.Exists(DataFolder))
                    Directory.CreateDirectory(DataFolder);

                if (!System.IO.File.Exists(DataFile))
                    System.IO.File.WriteAllText(DataFile, "[]");

                var json = System.IO.File.ReadAllText(DataFile);
                return JsonSerializer.Deserialize<List<Banner>>(json, _opts)
                       ?? new List<Banner>();
            }
        }

        private void Guardar(List<Banner> lista)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(lista, _opts);
                System.IO.File.WriteAllText(DataFile, json);
            }
        }
    }

    public class BannerForm
    {
        public IFormFile? Imagen { get; set; }
        public string? Seccion { get; set; }
        public int LocalId { get; set; }
        public int TecnicoId { get; set; }
        public int Dias { get; set; } = 7;
    }
}