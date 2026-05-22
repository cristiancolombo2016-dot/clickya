using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TecnicoController : ControllerBase
    {
        private string DataFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private string DataFile => Path.Combine(DataFolder, "tecnicos.json");

        private List<Tecnico> Leer()
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
            var json = System.IO.File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<List<Tecnico>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<Tecnico>();
        }

        private void Guardar(List<Tecnico> lista)
        {
            var json = JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(DataFile, json);
        }
        [HttpGet]
        public IActionResult GetAll() => Ok(Leer());

        [HttpGet("rubro/{rubro}")]
        public IActionResult GetPorRubro(string rubro)
        {
            var lista = Leer().Where(t => t.Activo &&
                t.Rubro.ToLower() == rubro.ToLower()).ToList();
            return Ok(lista);
        }

        [HttpGet("categoria/{rubro}")]
        public IActionResult GetPorCategoria(string rubro)
        {
            var lista = Leer()
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(t => t.EsPremium)
                .ToList();
            return Ok(lista);
        }

        [HttpGet("urgencias")]
        public IActionResult GetUrgencias()
        {
            var lista = Leer()
                .Where(t => t.Activo && t.EsPremium)
                .ToList();
            return Ok(lista);
        }

        [HttpPost]
        public IActionResult Crear([FromBody] Tecnico tecnico)
        {
            var lista = Leer();
            tecnico.Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1;
            lista.Add(tecnico);
            Guardar(lista);
            return Ok(tecnico);
        }

        [HttpPut("{id}")]
        public IActionResult Editar(int id, [FromBody] Tecnico tecnico)
        {
            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id);
            if (existente == null) return NotFound();

            existente.Nombre = tecnico.Nombre;
            existente.Rubro = tecnico.Rubro;
            existente.WhatsApp = tecnico.WhatsApp;
            existente.Activo = tecnico.Activo;
            existente.EsPremium = tecnico.EsPremium;
            existente.FechaPremium = tecnico.FechaPremium;
            existente.FotoPortada = tecnico.FotoPortada;
            existente.Logo = tecnico.Logo;
            existente.Ubicacion = tecnico.Ubicacion;
            existente.Direccion = tecnico.Direccion;
            existente.Descripcion = tecnico.Descripcion;
            existente.Instagram = tecnico.Instagram;
            existente.Latitud = tecnico.Latitud;
            existente.Longitud = tecnico.Longitud;
            if (!string.IsNullOrWhiteSpace(tecnico.Token))
                existente.Token = tecnico.Token;

            Guardar(lista);
            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id)
        {
            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.Id == id);
            if (existente == null) return NotFound();
            lista.Remove(existente);
            Guardar(lista);
            return Ok();
        }
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var tecnico = Leer().FirstOrDefault(t => t.Id == id);
            if (tecnico == null) return NotFound();
            return Ok(tecnico);
        }
        [HttpPost("{id}/portada")]
        public async Task<IActionResult> SubirPortada(int id, IFormFile archivo)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            var lista = Leer();
            var existente = lista.FirstOrDefault(t => t.Id == id);
            if (existente == null) return NotFound();
            existente.FotoPortada = $"/uploads/{fileName}";
            Guardar(lista);

            return Ok(new { portadaUrl = existente.FotoPortada });
        }

        [HttpPost("{id}/logo")]
        public async Task<IActionResult> SubirLogo(int id, IFormFile archivo)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            var lista = Leer();
            var existente = lista.FirstOrDefault(t => t.Id == id);
            if (existente == null) return NotFound();
            existente.Logo = $"/uploads/{fileName}";
            Guardar(lista);

            return Ok(new { logoUrl = existente.Logo });
        }
        [HttpGet("token/{token}")]
        public IActionResult GetPorToken(string token)
        {
            var lista = Leer();
            var tecnico = lista.FirstOrDefault(t => t.Token == token);
            if (tecnico == null) return NotFound();
            return Ok(tecnico);
        }
    }
}