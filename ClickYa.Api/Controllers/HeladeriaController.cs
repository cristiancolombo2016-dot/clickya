using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ClickYa.Api.Security;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HeladeriaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HeladeriaController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{comercioId}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int comercioId)
        {
            var h = await _db.Heladerias
                .Include(x => x.Sabores)
                .FirstOrDefaultAsync(x => x.ComercioId == comercioId);
            if (h == null) return Ok(new Heladeria { ComercioId = comercioId });
            return Ok(h);
        }

        [HttpPut("{comercioId}")]
        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.ComercioRole}")]
        public async Task<IActionResult> Put(int comercioId, [FromBody] Heladeria heladeria)
        {
            if (!User.CanAccess(SecurityDefaults.ComercioRole, comercioId))
                return Forbid();
            var existente = await _db.Heladerias
                .Include(x => x.Sabores)
                .FirstOrDefaultAsync(x => x.ComercioId == comercioId);

            if (existente == null)
            {
                heladeria.ComercioId = comercioId;
                _db.Heladerias.Add(heladeria);
            }
            else
            {
                existente.Sabores = heladeria.Sabores;
                existente.PrecioCuarto = heladeria.PrecioCuarto;
                existente.PrecioMedio = heladeria.PrecioMedio;
                existente.PrecioKilo = heladeria.PrecioKilo;
            }

            await _db.SaveChangesAsync();
            return Ok(heladeria);
        }
    }
}
