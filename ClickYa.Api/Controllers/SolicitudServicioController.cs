using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolicitudServicioController : ControllerBase
    {
        private string DataFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private string DataFile => Path.Combine(DataFolder, "solicitudes_servicio.json");
        private string UploadsFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        private List<SolicitudServicio> Leer()
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
            var json = System.IO.File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<List<SolicitudServicio>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<SolicitudServicio>();
        }

        private void Guardar(List<SolicitudServicio> lista)
        {
            var json = JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(DataFile, json);
        }

        [HttpGet]
        public IActionResult GetAll() => Ok(Leer().OrderByDescending(x => x.Fecha));

        [HttpGet("rubro/{rubro}")]
        public IActionResult GetPorRubro(string rubro)
        {
            var lista = Leer().Where(s => s.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(x => x.Fecha).ToList();
            return Ok(lista);
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Crear([FromForm] string rubro,
            [FromForm] string descripcion,
            [FromForm] string whatsAppCliente,
            IFormFile? imagen)
        {
            if (!Directory.Exists(UploadsFolder)) Directory.CreateDirectory(UploadsFolder);

            var imagenUrl = "";
            if (imagen != null && imagen.Length > 0)
            {
                var ext = Path.GetExtension(imagen.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(UploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await imagen.CopyToAsync(stream);
                imagenUrl = $"/uploads/{fileName}";
            }

            var lista = Leer();
            var nueva = new SolicitudServicio
            {
                Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1,
                Rubro = rubro,
                Descripcion = descripcion,
                WhatsAppCliente = whatsAppCliente,
                ImagenUrl = imagenUrl,
                Fecha = DateTime.Now,
                Estado = "Pendiente"
            };

            lista.Add(nueva);
            Guardar(lista);
            return Ok(nueva);
        }

        [HttpPut("{id}/estado")]
        public IActionResult CambiarEstado(int id, [FromBody] string estado)
        {
            var lista = Leer();
            var solicitud = lista.FirstOrDefault(x => x.Id == id);
            if (solicitud == null) return NotFound();
            solicitud.Estado = estado;
            Guardar(lista);
            return Ok(solicitud);
        }
    }
}