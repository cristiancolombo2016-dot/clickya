using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClickYa.Api.Models;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HeladeriaController : ControllerBase
    {
        private string DataFolder => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private string DataFile => Path.Combine(DataFolder, "heladerias.json");

        private List<Heladeria> Leer()
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
            var json = System.IO.File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<List<Heladeria>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<Heladeria>();
        }

        private void Guardar(List<Heladeria> lista)
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            var json = JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(DataFile, json);
        }

        [HttpGet("{comercioId}")]
        public IActionResult Get(int comercioId)
        {
            var lista = Leer();
            var h = lista.FirstOrDefault(x => x.ComercioId == comercioId);
            if (h == null) return Ok(new Heladeria { ComercioId = comercioId });
            return Ok(h);
        }

        [HttpPut("{comercioId}")]
        public IActionResult Put(int comercioId, [FromBody] Heladeria heladeria)
        {
            var lista = Leer();
            var existente = lista.FirstOrDefault(x => x.ComercioId == comercioId);
            if (existente == null)
            {
                heladeria.ComercioId = comercioId;
                lista.Add(heladeria);
            }
            else
            {
                existente.Sabores = heladeria.Sabores;
                existente.PrecioCuarto = heladeria.PrecioCuarto;
                existente.PrecioMedio = heladeria.PrecioMedio;
                existente.PrecioKilo = heladeria.PrecioKilo;
            }
            Guardar(lista);
            return Ok(heladeria);
        }
    }
}