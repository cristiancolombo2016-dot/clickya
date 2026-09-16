using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ClickYa.Api.Security;

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
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> GetActivos()
        {
            var lista = await _db.MensajesAdmin.Where(m => m.Activo).ToListAsync();
            return Ok(lista);
        }

        [HttpGet("tecnico/{tecnicoId}")]
        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        public async Task<IActionResult> GetParaTecnico(int tecnicoId)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, tecnicoId))
                return Forbid();
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
        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.ComercioRole}")]
        public async Task<IActionResult> GetParaComercio(int comercioId)
        {
            if (!User.CanAccess(SecurityDefaults.ComercioRole, comercioId))
                return Forbid();
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
        [Authorize(Roles = SecurityDefaults.AdminRole)]
        public async Task<IActionResult> Crear([FromBody] MensajeAdmin mensaje)
        {
            mensaje.Fecha = DateTime.UtcNow;
            mensaje.Activo = true;
            _db.MensajesAdmin.Add(mensaje);
            await _db.SaveChangesAsync();
            return Ok(mensaje);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SecurityDefaults.AdminRole)]
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
