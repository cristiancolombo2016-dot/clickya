using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public CategoriasController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads", "categorias");
        }

        [HttpGet("seccion/{seccion}")]
        public async Task<IActionResult> GetPorSeccion(string seccion)
        {
            var lista = await _db.Categorias
                .Where(c => c.Activo && c.Seccion.ToLower() == seccion.ToLower())
                .OrderBy(c => c.Orden)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("todos")]
        public async Task<IActionResult> GetTodos()
        {
            var lista = await _db.Categorias.ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Crear([FromForm] CategoriaForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Nombre)) return BadRequest("Falta nombre");
            if (form.Icono == null || form.Icono.Length == 0) return BadRequest("Falta icono");

            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);

            var ext = Path.GetExtension(form.Icono.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(_uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await form.Icono.CopyToAsync(stream);

            var nueva = new Categoria
            {
                Seccion = form.Seccion ?? "comidas",
                Nombre = form.Nombre,
                IconoUrl = $"/uploads/categorias/{fileName}",
                Orden = form.Orden,
                Activo = true
            };

            _db.Categorias.Add(nueva);
            await _db.SaveChangesAsync();
            return Ok(nueva);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Editar(int id, [FromForm] CategoriaForm form)
        {
            var existente = await _db.Categorias.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nombre = form.Nombre ?? existente.Nombre;
            existente.Orden = form.Orden > 0 ? form.Orden : existente.Orden;
            existente.Seccion = form.Seccion ?? existente.Seccion;

            if (form.Icono != null && form.Icono.Length > 0)
            {
                if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
                var ext = Path.GetExtension(form.Icono.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(_uploadsPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await form.Icono.CopyToAsync(stream);
                existente.IconoUrl = $"/uploads/categorias/{fileName}";
            }

            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var categoria = await _db.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();
            _db.Categorias.Remove(categoria);
            await _db.SaveChangesAsync();
            return Ok();
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