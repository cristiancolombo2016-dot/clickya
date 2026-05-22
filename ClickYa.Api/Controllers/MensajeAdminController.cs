using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MensajeAdminController : ControllerBase
    {
        private static readonly object _lock = new();
        private string DataFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private string DataFile => Path.Combine(DataFolder, "mensajes_admin.json");
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        [HttpGet("activos")]
        public IActionResult GetActivos()
        {
            var lista = Leer().Where(m => m.Activo).ToList();
            return Ok(lista);
        }

        [HttpGet("tecnico/{tecnicoId}")]
        public IActionResult GetParaTecnico(int tecnicoId)
        {
            var lista = Leer()
                .Where(m => m.Activo && (
                    m.Destino == "todos" ||
                    m.Destino == "tecnicos" ||
                    m.Destino == "servicios" ||
                    (m.Destino == "tecnico" && m.DestinoId == tecnicoId)
                ))
                .ToList();
            return Ok(lista);
        }

        [HttpGet("comercio/{comercioId}")]
        public IActionResult GetParaComercio(int comercioId)
        {
            var pathComercios = Path.Combine(DataFolder, "comercios.json");
            var comercios = System.IO.File.Exists(pathComercios)
                ? JsonSerializer.Deserialize<List<Comercio>>(
                    System.IO.File.ReadAllText(pathComercios), _opts) ?? new()
                : new List<Comercio>();

            var comercio = comercios.FirstOrDefault(c => c.Id == comercioId);
            var rubro = comercio?.Rubro?.ToLower() ?? "";
            var categoria = comercio?.Categoria?.ToLower() ?? "";

            var lista = Leer()
                .Where(m => m.Activo && (
                    m.Destino == "todos" ||
                    m.Destino == "comercios" ||
                    (m.Destino == "comercio" && m.DestinoId == comercioId) ||
                    (m.Destino == "comida" && rubro.Contains("comida")) ||
                    (m.Destino == "tiendas" && rubro.Contains("tienda")) ||
                    (m.Destino == "bares" && rubro.Contains("bar")) ||
                    (m.Destino == "heladerias" && categoria.Contains("helad"))
                ))
                .ToList();

            return Ok(lista);
        }

        [HttpPost]
        public IActionResult Crear([FromBody] MensajeAdmin mensaje)
        {
            var lista = Leer();
            mensaje.Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1;
            mensaje.Fecha = DateTime.Now;
            mensaje.Activo = true;
            lista.Add(mensaje);
            Guardar(lista);
            return Ok(mensaje);
        }

        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id)
        {
            var lista = Leer();
            var m = lista.FirstOrDefault(x => x.Id == id);
            if (m == null) return NotFound();
            lista.Remove(m);
            Guardar(lista);
            return Ok();
        }

        private List<MensajeAdmin> Leer()
        {
            lock (_lock)
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
                if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
                return JsonSerializer.Deserialize<List<MensajeAdmin>>(
                    System.IO.File.ReadAllText(DataFile), _opts) ?? new();
            }
        }

        private void Guardar(List<MensajeAdmin> lista)
        {
            lock (_lock)
            {
                System.IO.File.WriteAllText(DataFile,
                    JsonSerializer.Serialize(lista, _opts));
            }
        }
    }
}