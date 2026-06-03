using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BannersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public BannersController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.Banners
                .Where(b => b.Activo && b.Inicio.AddDays(b.Dias) >= DateTime.UtcNow)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("seccion/{seccion}")]
        public async Task<IActionResult> GetPorSeccion(string seccion)
        {
            var lista = await _db.Banners
                .Where(b => b.Activo &&
                       b.Seccion.ToLower() == seccion.ToLower() &&
                       b.Inicio.AddDays(b.Dias) >= DateTime.UtcNow)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("todos")]
        public async Task<IActionResult> GetTodos()
        {
            var lista = await _db.Banners.ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Crear([FromForm] BannerForm form)
        {
            if (form.Imagen == null || form.Imagen.Length == 0)
                return BadRequest("Falta imagen");

            if (!Directory.Exists(_uploadsPath))
                Directory.CreateDirectory(_uploadsPath);

            var ext = Path.GetExtension(form.Imagen.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(_uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await form.Imagen.CopyToAsync(stream);

            var nuevo = new Banner
            {
                Seccion = form.Seccion ?? "home",
                ImagenUrl = $"/uploads/{fileName}",
                LocalId = form.LocalId,
                TecnicoId = form.TecnicoId,
                Dias = form.Dias > 0 ? form.Dias : 7,
                Inicio = DateTime.UtcNow,
                Activo = true
            };

            _db.Banners.Add(nuevo);
            await _db.SaveChangesAsync();
            return Ok(nuevo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            _db.Banners.Remove(banner);
            await _db.SaveChangesAsync();
            return Ok();
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