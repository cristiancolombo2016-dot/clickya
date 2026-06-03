using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/solicitudes")]
    public class SolicitudesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SolicitudesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] SolicitudComercio solicitud)
        {
            try
            {
                // Verificar email duplicado (excepto el admin)
                if (!string.IsNullOrWhiteSpace(solicitud.Email) &&
                    solicitud.Email.ToLower() != "cristiancolombo2016@gmail.com" &&
                    await _db.Solicitudes.AnyAsync(s => s.Email.ToLower() == solicitud.Email.ToLower()))
                    return BadRequest("Ya existe un comercio con ese email");

                solicitud.CreatedAt = DateTime.UtcNow;
                var token = Guid.NewGuid().ToString("N");
                solicitud.Token = token;
                solicitud.Estado = "CONFIABLE";

                var nuevoComercio = new Comercio
                {
                    Nombre = solicitud.Nombre,
                    Rubro = solicitud.Rubro,
                    Categoria = solicitud.Categoria,
                    Descripcion = solicitud.Descripcion,
                    WhatsApp = solicitud.Telefono,
                    Ubicacion = "San Nicolás",
                    Estado = "Activo",
                    Token = token
                };

                _db.Comercios.Add(nuevoComercio);
                await _db.SaveChangesAsync();

                solicitud.ComercioId = nuevoComercio.Id;
                _db.Solicitudes.Add(solicitud);
                await _db.SaveChangesAsync();

                return Ok(new { token = token, comercioId = nuevoComercio.Id, nombre = solicitud.Nombre });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s =>
                s.Email.ToLower() == req.Email.ToLower() &&
                s.Password == req.Password &&
                s.Estado == "CONFIABLE");

            if (solicitud == null) return Unauthorized("Email o contraseña incorrectos");

            return Ok(new { token = solicitud.Token, comercioId = solicitud.ComercioId, nombre = solicitud.Nombre });
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lista = await _db.Solicitudes.ToListAsync();
            return Ok(lista);
        }

        [HttpPut("{id}/aprobar")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _db.Solicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            solicitud.Estado = "CONFIABLE";
            await _db.SaveChangesAsync();
            return Ok(new { token = solicitud.Token, comercioId = solicitud.ComercioId });
        }

        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetPorToken(string token)
        {
            var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.Token == token);
            if (solicitud == null) return NotFound("Token inválido");
            return Ok(new { comercioId = solicitud.ComercioId });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var solicitud = await _db.Solicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            _db.Solicitudes.Remove(solicitud);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}/bloquear")]
        public async Task<IActionResult> Bloquear(int id)
        {
            var solicitud = await _db.Solicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            solicitud.Estado = "BLOQUEADO";
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}