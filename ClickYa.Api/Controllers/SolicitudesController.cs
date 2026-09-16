using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] SolicitudRegistroRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Nombre) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    request.Password.Length < 8)
                    return BadRequest("Nombre, email y contraseña de al menos 8 caracteres son obligatorios.");

                // El email identifica de forma única al comercio para el inicio de sesión.
                var email = request.Email.Trim().ToLowerInvariant();
                if (await _db.Solicitudes.AnyAsync(s => s.Email.ToLower() == email))
                    return BadRequest("Ya existe un comercio con ese email");

                var solicitud = new SolicitudComercio
                {
                    Nombre = request.Nombre.Trim(),
                    Rubro = request.Rubro?.Trim() ?? "",
                    Categoria = request.Categoria?.Trim() ?? "",
                    Telefono = request.Telefono?.Trim() ?? "",
                    Descripcion = request.Descripcion?.Trim() ?? "",
                    Email = email,
                    Password = PasswordSecurity.Hash(request.Password)
                };
                solicitud.CreatedAt = DateTime.UtcNow;
                solicitud.Token = "";
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
                    Token = ""
                };

                _db.Comercios.Add(nuevoComercio);
                await _db.SaveChangesAsync();

                solicitud.ComercioId = nuevoComercio.Id;
                _db.Solicitudes.Add(solicitud);
                await _db.SaveChangesAsync();

                return Ok(new { comercioId = nuevoComercio.Id, nombre = solicitud.Nombre, requiereLogin = true });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<SolicitudesController>>()
                    .LogError(ex, "No se pudo registrar el comercio");
                return StatusCode(500, "No se pudo completar el registro.");
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Unauthorized("Email o contraseña incorrectos");

            var throttle = HttpContext.RequestServices.GetRequiredService<LoginThrottle>();
            var throttleKey = $"comercio:{HttpContext.Connection.RemoteIpAddress}:{req.Email.Trim().ToLowerInvariant()}";
            if (throttle.IsBlocked(throttleKey))
                return StatusCode(StatusCodes.Status429TooManyRequests, "Demasiados intentos. Probá nuevamente más tarde.");

            var email = req.Email.Trim().ToLowerInvariant();
            var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s =>
                s.Email.ToLower() == email && s.Estado == "CONFIABLE");

            var needsUpgrade = false;
            var passwordOk = solicitud != null &&
                             PasswordSecurity.Verify(req.Password, solicitud.Password, out needsUpgrade);

            if (!passwordOk || solicitud == null)
            {
                throttle.RegisterFailure(throttleKey);
                return Unauthorized("Email o contraseña incorrectos");
            }

            if (needsUpgrade)
            {
                solicitud.Password = PasswordSecurity.Hash(req.Password);
                await _db.SaveChangesAsync();
            }

            throttle.RegisterSuccess(throttleKey);
            var tokens = HttpContext.RequestServices.GetRequiredService<AccessTokenService>();
            var tickets = HttpContext.RequestServices.GetRequiredService<WebLoginTicketService>();
            var accessToken = tokens.Create(
                SecurityDefaults.ComercioRole,
                solicitud.ComercioId,
                solicitud.Nombre);
            return Ok(new
            {
                dashboardTicket = tickets.Issue(accessToken),
                comercioId = solicitud.ComercioId,
                nombre = solicitud.Nombre
            });
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lista = await _db.Solicitudes.ToListAsync();
            return Ok(lista);
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpPut("{id}/aprobar")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _db.Solicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            solicitud.Estado = "CONFIABLE";
            await _db.SaveChangesAsync();
            return Ok(new { comercioId = solicitud.ComercioId });
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var solicitud = await _db.Solicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            _db.Solicitudes.Remove(solicitud);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
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

    public sealed class SolicitudRegistroRequest
    {
        public string Nombre { get; set; } = "";
        public string? Rubro { get; set; }
        public string? Categoria { get; set; }
        public string? Telefono { get; set; }
        public string? Descripcion { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
