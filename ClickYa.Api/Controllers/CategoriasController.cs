using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;


namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private static readonly object _lock = new();

        private string DataFolder => Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "data");

        private string DataFile => Path.Combine(DataFolder, "categorias.json");

        private string UploadsFolder => Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "uploads", "categorias");

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        [HttpGet("seccion/{seccion}")]
        public IActionResult GetPorSeccion(string seccion)
        {
            var lista = Leer();

            var filtradas = lista
                .Where(c => c.Activo &&
                            c.Seccion.ToLower() == seccion.ToLower())
                .OrderBy(c => c.Orden)
                .ToList();

            return Ok(filtradas);
        }

        [HttpGet("todos")]
        public IActionResult GetTodos()
        {
            return Ok(Leer());
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Crear([FromForm] CategoriaForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Nombre))
                return BadRequest("Falta nombre");

            if (form.Icono == null || form.Icono.Length == 0)
                return BadRequest("Falta icono");

            if (!Directory.Exists(UploadsFolder))
                Directory.CreateDirectory(UploadsFolder);

            var ext = Path.GetExtension(form.Icono.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(UploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await form.Icono.CopyToAsync(stream);

            var lista = Leer();

            var nueva = new Categoria
            {
                Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1,
                Seccion = form.Seccion ?? "comidas",
                Nombre = form.Nombre,
                IconoUrl = $"/uploads/categorias/{fileName}",
                Orden = form.Orden,
                Activo = true
            };

            lista.Add(nueva);
            Guardar(lista);

            return Ok(nueva);
        }
        [HttpPut("{id}")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Editar(int id, [FromForm] CategoriaForm form)
        {
            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id);
            if (existente == null) return NotFound();

            existente.Nombre = form.Nombre ?? existente.Nombre;
            existente.Orden = form.Orden > 0 ? form.Orden : existente.Orden;
            existente.Seccion = form.Seccion ?? existente.Seccion;

            if (form.Icono != null && form.Icono.Length > 0)
            {
                if (!Directory.Exists(UploadsFolder))
                    Directory.CreateDirectory(UploadsFolder);

                var ext = Path.GetExtension(form.Icono.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(UploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await form.Icono.CopyToAsync(stream);

                existente.IconoUrl = $"/uploads/categorias/{fileName}";
            }

            Guardar(lista);
            return Ok(existente);
        }
        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id)
        {
            var lista = Leer();
            var categoria = lista.FirstOrDefault(x => x.Id == id);

            if (categoria == null)
                return NotFound();

            lista.Remove(categoria);
            Guardar(lista);

            return Ok();
        }

        private List<Categoria> Leer()
        {
            lock (_lock)
            {
                if (!Directory.Exists(DataFolder))
                    Directory.CreateDirectory(DataFolder);

                if (!System.IO.File.Exists(DataFile))
                    System.IO.File.WriteAllText(DataFile, "[]");

                var json = System.IO.File.ReadAllText(DataFile);

                return JsonSerializer.Deserialize<List<Categoria>>(json, _opts)
                       ?? new List<Categoria>();
            }
        }

        private void Guardar(List<Categoria> lista)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(lista, _opts);
                System.IO.File.WriteAllText(DataFile, json);
            }
        }
    }

    public class CategoriaForm
    {
        public IFormFile? Icono { get; set; }
        public string? Seccion { get; set; }
        public string Nombre { get; set; } = "";
        public int Orden { get; set; }
    }
}