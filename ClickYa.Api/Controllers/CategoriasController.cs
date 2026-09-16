using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ClickYa.Api.Security;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SafeImageStorage _images;

        public CategoriasController(AppDbContext db, SafeImageStorage images)
        {
            _db = db;
            _images = images;
        }

        [HttpGet("seccion/{seccion}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPorSeccion(string seccion)
        {
            var lista = await _db.Categorias
                .Where(c => c.Activo && c.Seccion.ToLower() == seccion.ToLower())
                .OrderBy(c => c.Orden)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("todos")]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> GetTodos()
        {
            var lista = await _db.Categorias.ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [RequestSizeLimit(12_000_000)]
        public async Task<IActionResult> Crear([FromForm] CategoriaForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Nombre)) return BadRequest("Falta nombre");
            if (form.Icono == null || form.Icono.Length == 0) return BadRequest("Falta icono");

            var iconoUrl = await _images.SaveAsync(form.Icono, "categorias");
            if (iconoUrl == null) return BadRequest("El ícono debe ser JPG, PNG o WEBP y pesar hasta 10 MB.");

            var nueva = new Categoria
            {
                Seccion = form.Seccion ?? "comidas",
                Nombre = form.Nombre,
                IconoUrl = iconoUrl,
                Orden = form.Orden,
                Activo = true
            };

            _db.Categorias.Add(nueva);
            try { await _db.SaveChangesAsync(); }
            catch { _images.Delete(iconoUrl); throw; }
            return Ok(nueva);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [RequestSizeLimit(12_000_000)]
        public async Task<IActionResult> Editar(int id, [FromForm] CategoriaForm form)
        {
            var existente = await _db.Categorias.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nombre = form.Nombre ?? existente.Nombre;
            existente.Orden = form.Orden > 0 ? form.Orden : existente.Orden;
            existente.Seccion = form.Seccion ?? existente.Seccion;

            if (form.Icono != null && form.Icono.Length > 0)
            {
                var iconoUrl = await _images.SaveAsync(form.Icono, "categorias");
                if (iconoUrl == null) return BadRequest("El ícono debe ser JPG, PNG o WEBP y pesar hasta 10 MB.");
                existente.IconoUrl = iconoUrl;
                try { await _db.SaveChangesAsync(); }
                catch { _images.Delete(iconoUrl); throw; }
                return Ok(existente);
            }

            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var categoria = await _db.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();
            if (await _db.Tecnicos.AnyAsync(t => t.CategoriaId == id) ||
                await _db.Urgencias.AnyAsync(u => u.CategoriaId == id))
                return Conflict("La categoría está asociada a técnicos o urgencias y no puede eliminarse.");
            _db.Categorias.Remove(categoria);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Conflict("La categoría está en uso y no puede eliminarse.");
            }
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
