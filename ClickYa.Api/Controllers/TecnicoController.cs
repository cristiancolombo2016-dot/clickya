using Microsoft.AspNetCore.Mvc;
using ClickYa.Api.Models;
using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TecnicoController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SafeImageStorage _images;

        public TecnicoController(AppDbContext db, SafeImageStorage images)
        {
            _db = db;
            _images = images;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = User.IsInRole(SecurityDefaults.AdminRole)
                ? _db.Tecnicos.AsQueryable()
                : _db.Tecnicos.Where(t => t.Activo);
            var lista = await ProyectarPublicos(query).ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("rubro/{rubro}")]
        public async Task<IActionResult> GetPorRubro(string rubro)
        {
            var lista = await ProyectarPublicos(_db.Tecnicos
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())).ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("categoria/{rubro}")]
        public async Task<IActionResult> GetPorCategoria(string rubro)
        {
            var desde = DateTime.UtcNow.AddDays(-PremiumMembership.DuracionDias);
            var ahora = DateTime.UtcNow;
            var lista = await ProyectarPublicos(_db.Tecnicos
                .Where(t => t.Activo && t.Rubro.ToLower() == rubro.ToLower())
                .OrderByDescending(t => t.EsPremium && t.FechaPremium > desde && t.FechaPremium <= ahora)).ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("categoria/{categoriaId:int}")]
        public async Task<IActionResult> GetPorCategoriaId(int categoriaId)
        {
            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaId &&
                c.Activo && c.Seccion.ToLower() == "servicios");
            if (categoria == null) return NotFound();
            var desde = DateTime.UtcNow.AddDays(-PremiumMembership.DuracionDias);
            var ahora = DateTime.UtcNow;
            var lista = await ProyectarPublicos(_db.Tecnicos.Where(t => t.Activo &&
                    (t.CategoriaId == categoriaId || (t.CategoriaId == null && t.Rubro.ToLower() == categoria.Nombre.ToLower())))
                .OrderByDescending(t => t.EsPremium && t.FechaPremium > desde && t.FechaPremium <= ahora)).ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("urgencias")]
        [Obsolete("Las urgencias se consultan en /api/Urgencia/disponibles.")]
        public async Task<IActionResult> GetUrgencias()
        {
            var desde = DateTime.UtcNow.AddDays(-PremiumMembership.DuracionDias);
            var ahora = DateTime.UtcNow;
            var lista = await _db.Tecnicos
                .Where(t => t.Activo && t.EsPremium && t.FechaPremium > desde && t.FechaPremium <= ahora)
                .Select(t => new { t.Id, t.Nombre, t.Rubro, t.Logo }).ToListAsync();
            return Ok(lista);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var puedeVerInactivo = User.IsInRole(SecurityDefaults.AdminRole);
            var tecnico = await ProyectarPublicos(_db.Tecnicos.Where(t => t.Id == id && (t.Activo || puedeVerInactivo)))
                .FirstOrDefaultAsync();
            if (tecnico == null) return NotFound();
            return Ok(tecnico);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/calificaciones")]
        public async Task<IActionResult> GetCalificaciones(int id)
        {
            if (!await _db.Tecnicos.AnyAsync(t => t.Id == id && t.Activo)) return NotFound();
            var opiniones = await _db.CalificacionesServicio.Where(c => c.TecnicoId == id)
                .OrderByDescending(c => c.FechaCreacion)
                .Select(c => new { c.Estrellas, c.Comentario, c.FechaCreacion }).ToListAsync();
            return Ok(new
            {
                PromedioEstrellas = opiniones.Count == 0 ? 0 : opiniones.Average(o => o.Estrellas),
                CantidadOpiniones = opiniones.Count,
                Opiniones = opiniones.Where(o => !string.IsNullOrWhiteSpace(o.Comentario))
            });
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Tecnico tecnico)
        {
            var categoria = await ResolverCategoria(tecnico.CategoriaId, tecnico.Rubro);
            if (categoria == null) return BadRequest("Seleccioná una categoría válida de Servicios.");
            tecnico.CategoriaId = categoria.Id;
            tecnico.Rubro = categoria.Nombre;
            tecnico.Token = "";
            _db.Tecnicos.Add(tecnico);
            await _db.SaveChangesAsync();
            return Ok(tecnico);
        }

        [AllowAnonymous]
        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] TecnicoRegistroRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre) ||
                string.IsNullOrWhiteSpace(request.WhatsApp))
                return BadRequest("Nombre, rubro y WhatsApp son obligatorios.");

            var categoria = await ResolverCategoria(request.CategoriaId, request.Rubro);
            if (categoria == null) return BadRequest("Seleccioná una categoría válida de Servicios.");

            var tecnico = new Tecnico
            {
                Nombre = request.Nombre.Trim(),
                Rubro = categoria.Nombre,
                CategoriaId = categoria.Id,
                WhatsApp = request.WhatsApp.Trim(),
                Token = "",
                Activo = true,
                EsPremium = false
            };

            _db.Tecnicos.Add(tecnico);
            await _db.SaveChangesAsync();
            var tokens = HttpContext.RequestServices.GetRequiredService<AccessTokenService>();
            var tickets = HttpContext.RequestServices.GetRequiredService<WebLoginTicketService>();
            var accessToken = tokens.Create(SecurityDefaults.TecnicoRole, tecnico.Id, tecnico.Nombre);
            return Ok(new { tecnico.Id, tecnico.Nombre, dashboardTicket = tickets.Issue(accessToken) });
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpPost("{id:int}/access-ticket")]
        public async Task<IActionResult> GenerarAcceso(int id)
        {
            var tecnico = await _db.Tecnicos.FindAsync(id);
            if (tecnico == null) return NotFound();

            var tokens = HttpContext.RequestServices.GetRequiredService<AccessTokenService>();
            var tickets = HttpContext.RequestServices.GetRequiredService<WebLoginTicketService>();
            var accessToken = tokens.Create(SecurityDefaults.TecnicoRole, tecnico.Id, tecnico.Nombre);
            return Ok(new { dashboardTicket = tickets.Issue(accessToken) });
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] Tecnico tecnico)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, id)) return Forbid();
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nombre = tecnico.Nombre;
            var categoria = await ResolverCategoria(tecnico.CategoriaId, tecnico.Rubro);
            if (categoria == null) return BadRequest("Seleccioná una categoría válida de Servicios.");
            existente.CategoriaId = categoria.Id;
            existente.Rubro = categoria.Nombre;
            existente.WhatsApp = tecnico.WhatsApp;
            if (User.IsInRole(SecurityDefaults.AdminRole))
            {
                existente.Activo = tecnico.Activo;
                existente.EsPremium = tecnico.EsPremium;
                existente.FechaPremium = tecnico.FechaPremium;
            }
            existente.FotoPortada = tecnico.FotoPortada;
            existente.Logo = tecnico.Logo;
            existente.Ubicacion = tecnico.Ubicacion;
            existente.Direccion = tecnico.Direccion;
            existente.Descripcion = tecnico.Descripcion;
            existente.Instagram = tecnico.Instagram;
            existente.Latitud = tecnico.Latitud;
            existente.Longitud = tecnico.Longitud;
            await _db.SaveChangesAsync();
            return Ok(existente);
        }

        [Authorize(Roles = SecurityDefaults.AdminRole)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            _db.Tecnicos.Remove(existente);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Conflict("El técnico tiene ofertas o calificaciones y no puede eliminarse. Podés darlo de baja.");
            }
            return Ok();
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPost("{id}/portada")]
        [RequestSizeLimit(12_000_000)]
        public async Task<IActionResult> SubirPortada(int id, IFormFile archivo)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, id)) return Forbid();
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            var imagenUrl = await _images.SaveAsync(archivo);
            if (imagenUrl == null) return BadRequest("La portada debe ser JPG, PNG o WEBP y pesar hasta 10 MB.");
            existente.FotoPortada = imagenUrl;
            try { await _db.SaveChangesAsync(); }
            catch { _images.Delete(imagenUrl); throw; }
            return Ok(new { portadaUrl = existente.FotoPortada });
        }

        [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
        [HttpPost("{id}/logo")]
        [RequestSizeLimit(12_000_000)]
        public async Task<IActionResult> SubirLogo(int id, IFormFile archivo)
        {
            if (!User.CanAccess(SecurityDefaults.TecnicoRole, id)) return Forbid();
            var existente = await _db.Tecnicos.FindAsync(id);
            if (existente == null) return NotFound();
            var imagenUrl = await _images.SaveAsync(archivo);
            if (imagenUrl == null) return BadRequest("El logo debe ser JPG, PNG o WEBP y pesar hasta 10 MB.");
            existente.Logo = imagenUrl;
            try { await _db.SaveChangesAsync(); }
            catch { _images.Delete(imagenUrl); throw; }
            return Ok(new { logoUrl = existente.Logo });
        }

        private async Task<Categoria?> ResolverCategoria(int? categoriaId, string? rubro)
        {
            if (categoriaId is > 0)
                return await _db.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaId && c.Activo &&
                    c.Seccion.ToLower() == "servicios");
            if (string.IsNullOrWhiteSpace(rubro)) return null;
            var normalizado = rubro.Trim().ToLower();
            return await _db.Categorias.FirstOrDefaultAsync(c => c.Activo &&
                c.Seccion.ToLower() == "servicios" && c.Nombre.ToLower() == normalizado);
        }

        private IQueryable<TecnicoPublicoDto> ProyectarPublicos(IQueryable<Tecnico> query)
        {
            var desde = DateTime.UtcNow.AddDays(-PremiumMembership.DuracionDias);
            var ahora = DateTime.UtcNow;
            return query.Select(t => new TecnicoPublicoDto
            {
                Id = t.Id, Nombre = t.Nombre,
                Rubro = _db.Categorias.Where(c => c.Id == t.CategoriaId)
                    .Select(c => c.Nombre).FirstOrDefault() ?? t.Rubro,
                CategoriaId = t.CategoriaId,
                WhatsApp = t.WhatsApp, Activo = t.Activo, EsPremium = t.EsPremium,
                FechaPremium = t.FechaPremium,
                PremiumVigente = t.Activo && t.EsPremium && t.FechaPremium > desde && t.FechaPremium <= ahora,
                FotoPortada = t.FotoPortada, Logo = t.Logo, Ubicacion = t.Ubicacion,
                Direccion = t.Direccion, Descripcion = t.Descripcion, Instagram = t.Instagram,
                Latitud = t.Latitud, Longitud = t.Longitud,
                PromedioEstrellas = _db.CalificacionesServicio.Where(c => c.TecnicoId == t.Id)
                    .Select(c => (double?)c.Estrellas).Average() ?? 0,
                CantidadOpiniones = _db.CalificacionesServicio.Count(c => c.TecnicoId == t.Id)
            });
        }
    }

    public sealed class TecnicoRegistroRequest
    {
        public string Nombre { get; set; } = "";
        public string Rubro { get; set; } = "";
        public int? CategoriaId { get; set; }
        public string WhatsApp { get; set; } = "";
    }

    public sealed class TecnicoPublicoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Rubro { get; set; } = "";
        public int? CategoriaId { get; set; }
        public string WhatsApp { get; set; } = "";
        public bool Activo { get; set; }
        public bool EsPremium { get; set; }
        public DateTime? FechaPremium { get; set; }
        public bool PremiumVigente { get; set; }
        public string FotoPortada { get; set; } = "";
        public string Logo { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Instagram { get; set; } = "";
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public double PromedioEstrellas { get; set; }
        public int CantidadOpiniones { get; set; }
    }
}
