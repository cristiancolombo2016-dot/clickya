using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComercioController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly string _uploadsPath;

        public ComercioController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        }

        private static (double lat, double lng) ExtraerCoordenadas(string ubicacion)
        {
            if (string.IsNullOrWhiteSpace(ubicacion)) return (0, 0);
            var match = System.Text.RegularExpressions.Regex.Match(
                ubicacion, @"(-?\d+\.\d+),(-?\d+\.\d+)");
            if (match.Success &&
                double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
                return (lat, lng);
            return (0, 0);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var comercio = await _db.Comercios.FirstOrDefaultAsync();
            if (comercio == null) return Ok(new Comercio());
            return Ok(comercio);
        }

        [HttpGet("todos")]
        public async Task<IActionResult> GetTodos()
        {
            var lista = await _db.Comercios.ToListAsync();
            return Ok(lista);
        }

        [HttpGet("rubro/{rubro}")]
        public async Task<IActionResult> GetPorRubro(string rubro)
        {
            var lista = await _db.Comercios
                .Where(x => x.Rubro.ToLower() == rubro.ToLower())
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var comercio = await _db.Comercios.FindAsync(id);
            if (comercio == null) return NotFound("No existe el comercio.");
            return Ok(comercio);
        }

        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetPorToken(string token)
        {
            var comercio = await _db.Comercios.FirstOrDefaultAsync(x => x.Token == token);
            if (comercio == null) return NotFound("Token inválido");
            return Ok(comercio);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutById(int id, [FromBody] Comercio comercioNuevo)
        {
            if (comercioNuevo == null) return BadRequest();
            var comercio = await _db.Comercios.FindAsync(id);
            if (comercio == null) return NotFound("No existe el comercio.");
            comercio.Nombre = comercioNuevo.Nombre;
            comercio.Descripcion = comercioNuevo.Descripcion;
            comercio.WhatsApp = new string((comercioNuevo.WhatsApp ?? "").Where(char.IsDigit).ToArray());
            comercio.Instagram = comercioNuevo.Instagram;
            comercio.Ubicacion = comercioNuevo.Ubicacion;
            var coords = ExtraerCoordenadas(comercioNuevo.Ubicacion);
            comercio.Latitud = coords.lat;
            comercio.Longitud = coords.lng;
            comercio.Correo = comercioNuevo.Correo;
            comercio.Horarios = comercioNuevo.Horarios;
            comercio.Rubro = comercioNuevo.Rubro;
            comercio.Estado = comercioNuevo.Estado;
            await _db.SaveChangesAsync();
            return Ok(comercio);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> Crear([FromBody] Comercio nuevo)
        {
            if (nuevo == null) return BadRequest();
            nuevo.WhatsApp = new string((nuevo.WhatsApp ?? "").Where(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(nuevo.Instagram) && !nuevo.Instagram.StartsWith("http"))
                nuevo.Instagram = "https://instagram.com/" + nuevo.Instagram.Replace("@", "");
            var coords = ExtraerCoordenadas(nuevo.Ubicacion);
            nuevo.Latitud = coords.lat;
            nuevo.Longitud = coords.lng;
            _db.Comercios.Add(nuevo);
            await _db.SaveChangesAsync();
            return Ok(nuevo);
        }

        [HttpPost("{id:int}/portada")]
        public async Task<IActionResult> SubirPortadaPorId(int id, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0) return BadRequest("Archivo inválido");
            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var esImagen = new[] { ".jpg", ".jpeg", ".png", ".webp", ".jfif" }.Contains(extension);
            var esVideo = extension == ".mp4";
            if (!esImagen && !esVideo) return BadRequest("Solo se permiten imágenes o videos MP4");
            var comercio = await _db.Comercios.FindAsync(id);
            if (comercio == null) return NotFound("No existe el comercio.");
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(_uploadsPath, nombreArchivo);
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                await archivo.CopyToAsync(stream);
            comercio.PortadaUrl = $"/uploads/{nombreArchivo}";
            comercio.PortadaTipo = esImagen ? "imagen" : "video";
            await _db.SaveChangesAsync();
            return Ok(comercio);
        }

        [HttpPost("{id:int}/logo")]
        public async Task<IActionResult> SubirLogoPorId(int id, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0) return BadRequest("Archivo inválido");
            if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var esImagen = new[] { ".jpg", ".jpeg", ".png", ".webp", ".jfif" }.Contains(extension);
            if (!esImagen) return BadRequest("El logo debe ser una imagen");
            var comercio = await _db.Comercios.FindAsync(id);
            if (comercio == null) return NotFound("No existe el comercio.");
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(_uploadsPath, nombreArchivo);
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                await archivo.CopyToAsync(stream);
            comercio.LogoUrl = $"/uploads/{nombreArchivo}";
            await _db.SaveChangesAsync();
            return Ok(comercio);
        }

        [HttpPut("{id:int}/destacado")]
        public async Task<IActionResult> ToggleDestacado(int id, [FromBody] bool esDestacado)
        {
            var comercio = await _db.Comercios.FindAsync(id);
            if (comercio == null) return NotFound();
            comercio.EsDestacado = esDestacado;
            await _db.SaveChangesAsync();
            return Ok(comercio);
        }

        [HttpGet("destacados")]
        public async Task<IActionResult> GetDestacados()
        {
            var lista = await _db.Comercios.Where(x => x.EsDestacado).ToListAsync();
            return Ok(lista);
        }
    }
}