using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrgenciaController : ControllerBase
    {
        private readonly string _rutaUrgencias;
        private readonly JsonSerializerOptions _opts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public UrgenciaController(IWebHostEnvironment env)
        {
            _rutaUrgencias = Path.Combine(env.WebRootPath, "data", "urgencias.json");
        }

        private List<SolicitudUrgencia> Cargar()
        {
            if (!System.IO.File.Exists(_rutaUrgencias)) return new();
            var json = System.IO.File.ReadAllText(_rutaUrgencias);
            return JsonSerializer.Deserialize<List<SolicitudUrgencia>>(json, _opts) ?? new();
        }

        private void Guardar(List<SolicitudUrgencia> lista)
        {
            var carpeta = Path.GetDirectoryName(_rutaUrgencias)!;
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            System.IO.File.WriteAllText(_rutaUrgencias, JsonSerializer.Serialize(lista, _opts));
        }

        [HttpPost("foto")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubirFoto(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Sin archivo");
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await archivo.CopyToAsync(stream);
            return Ok(new { url = $"/uploads/{fileName}" });
        }

        [HttpPost]
        public IActionResult Crear([FromBody] SolicitudUrgencia solicitud)
        {
            var lista = Cargar();
            solicitud.Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1;
            solicitud.Fecha = DateTime.Now;
            solicitud.Estado = "Nueva";
            lista.Add(solicitud);
            Guardar(lista);
            return Ok(solicitud);
        }

        [HttpGet("tecnico/{tecnicoId}")]
        public IActionResult GetPorTecnico(int tecnicoId)
        {
            var lista = Cargar();
            var resultado = lista
                .Where(x => x.TecnicoId == tecnicoId)
                .OrderByDescending(x => x.Fecha)
                .ToList();
            return Ok(resultado);
        }

        [HttpPut("{id}/estado")]
        public IActionResult CambiarEstado(int id, [FromBody] string estado)
        {
            var lista = Cargar();
            var item = lista.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();
            item.Estado = estado;
            Guardar(lista);
            return Ok(item);
        }
    }

    public class SolicitudUrgencia
    {
        public int Id { get; set; }
        public int TecnicoId { get; set; }
        public string Rubro { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string WhatsAppCliente { get; set; } = "";
        public string? FotoUrl { get; set; }
        public string Estado { get; set; } = "Nueva";
        public DateTime Fecha { get; set; }
    }
}