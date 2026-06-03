using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MensajeAdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MensajeAdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("activos")]
        public async Task<IActionResult> GetActivos()
        {
            var lista = await _db.MensajesAdmin.Where(m => m.Activo).ToListAsync();
            return Ok(lista);
        }

        [HttpGet("tecnico/{tecnicoId}")]
        public async Task<IActionResult> GetParaTecnico(int tecnicoId)
        {
            var lista = await _db.MensajesAdmin
                .Where(m => m.Activo && (
                    m.Destino == "todos" ||
                    m.Destino == "tecnicos" ||
                    m.Destino == "servicios" ||
                    (m.Destino == "tecnico" && m.DestinoId == tecnicoId)
                ))
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("comercio/{comercioId}")]
        public async Task<IActionResult> GetParaComercio(int comercioId)
        {
            var comercio = await _db.Comercios.FindAsync(comercioId);
            var rubro = comercio?.Rubro?.ToLower() ?? "";
            var categoria = comercio?.Categoria?.ToLower() ?? "";

            var lista = await _db.MensajesAdmin
                .Where(m => m.Activo && (
                    m.Destino == "todos" ||
                    m.Destino == "comercios" ||
                    (m.Destino == "comercio" && m.DestinoId == comercioId) ||
                    (m.Destino == "comida" && rubro.Contains("comida")) ||
                    (m.Destino == "tiendas" && rubro.Contains("tienda")) ||
                    (m.Destino == "bares" && rubro.Contains("bar")) ||
                    (m.Destino == "heladerias" && categoria.Contains("helad"))
                ))
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] MensajeAdmin mensaje)
        {
            mensaje.Fecha = DateTime.UtcNow;
            mensaje.Activo = true;
            _db.MensajesAdmin.Add(mensaje);
            await _db.SaveChangesAsync();
            return Ok(mensaje);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var mensaje = await _db.MensajesAdmin.FindAsync(id);
            if (mensaje == null) return NotFound();
            _db.MensajesAdmin.Remove(mensaje);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}