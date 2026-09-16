using System.Security.Claims;
using ClickYa.Api.Models;
using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrgenciaController : ControllerBase
{
    private const string ClientTokenHeader = "X-ClickYa-Solicitud-Token";
    private readonly AppDbContext _db;
    private readonly SensitiveDataProtector _protector;
    private readonly AnonymousRequestTokenService _tokens;
    private readonly SafeImageStorage _images;

    public UrgenciaController(AppDbContext db, SensitiveDataProtector protector,
        AnonymousRequestTokenService tokens, SafeImageStorage images)
    {
        _db = db;
        _protector = protector;
        _tokens = tokens;
        _images = images;
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("urgencias-anonimas")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> Publicar([FromForm] PublicarUrgenciaForm form)
    {
        if (form.CategoriaId <= 0 || string.IsNullOrWhiteSpace(form.Descripcion) ||
            string.IsNullOrWhiteSpace(form.ZonaBarrio) || string.IsNullOrWhiteSpace(form.DireccionExacta) ||
            string.IsNullOrWhiteSpace(form.ContactoCliente))
            return BadRequest("Completá rubro, descripción, zona, dirección y contacto.");

        var descripcion = form.Descripcion.Trim();
        var zona = form.ZonaBarrio.Trim();
        var direccion = form.DireccionExacta.Trim();
        var contacto = form.ContactoCliente.Trim();
        if (descripcion.Length is < 10 or > 1000 || zona.Length > 120 ||
            direccion.Length is < 5 or > 300 || contacto.Length is < 6 or > 40)
            return BadRequest("Los datos de la urgencia no tienen una longitud válida.");

        var categoria = await _db.Categorias.FirstOrDefaultAsync(c =>
            c.Id == form.CategoriaId && c.Activo && c.Seccion.ToLower() == "servicios");
        if (categoria == null) return BadRequest("El rubro seleccionado no está disponible.");

        string? fotoUrl = null;
        if (form.Foto is { Length: > 0 })
        {
            fotoUrl = await _images.SaveAsync(form.Foto);
            if (fotoUrl == null) return BadRequest("La foto debe ser JPG, PNG o WEBP y pesar hasta 10 MB.");
        }

        var rawToken = _tokens.Create();
        var urgencia = new SolicitudUrgencia
        {
            CategoriaId = categoria.Id,
            Rubro = categoria.Nombre,
            Descripcion = descripcion,
            ZonaBarrio = zona,
            DireccionExactaProtegida = _protector.Protect(direccion),
            ContactoClienteProtegido = _protector.Protect(contacto),
            TokenClienteHash = _tokens.Hash(rawToken),
            WhatsAppCliente = "",
            FotoUrl = fotoUrl,
            Estado = UrgenciaEstados.Publicada,
            Fecha = DateTime.UtcNow,
            TecnicoId = null
        };

        try
        {
            _db.Urgencias.Add(urgencia);
            await _db.SaveChangesAsync();
        }
        catch
        {
            _images.Delete(fotoUrl);
            throw;
        }

        return Ok(new PublicarUrgenciaResponse(urgencia.Id, rawToken, urgencia.Estado, urgencia.Fecha));
    }

    [HttpPost("foto")]
    [AllowAnonymous]
    [EnableRateLimiting("urgencias-anonimas")]
    [Obsolete("La fotografía debe enviarse junto con POST /api/Urgencia.")]
    public IActionResult SubirFotoAnterior() => StatusCode(StatusCodes.Status410Gone,
        "Actualizá la aplicación: la foto ahora se envía junto con la urgencia.");

    [HttpGet("disponibles")]
    [Authorize(Roles = SecurityDefaults.TecnicoRole)]
    public async Task<IActionResult> Disponibles()
    {
        var tecnico = await TecnicoActual();
        if (tecnico == null) return Unauthorized();
        if (!PremiumMembership.IsVigente(tecnico, DateTime.UtcNow)) return Forbid();

        var categoriaId = tecnico.CategoriaId;
        if (!categoriaId.HasValue)
            categoriaId = await _db.Categorias
                .Where(c => c.Activo && c.Seccion.ToLower() == "servicios" &&
                            c.Nombre.ToLower() == tecnico.Rubro.ToLower())
                .Select(c => (int?)c.Id).FirstOrDefaultAsync();
        if (!categoriaId.HasValue) return Ok(Array.Empty<object>());

        var lista = await _db.Urgencias
            .Where(u => u.CategoriaId == categoriaId && u.Estado == UrgenciaEstados.Publicada &&
                        u.OfertaSeleccionadaId == null)
            .OrderByDescending(u => u.Fecha)
            .Select(u => new
            {
                u.Id, u.Rubro, u.Descripcion, u.ZonaBarrio, u.FotoUrl, u.Fecha,
                YaOferto = _db.OfertasUrgencia.Any(o => o.UrgenciaId == u.Id && o.TecnicoId == tecnico.Id)
            }).ToListAsync();
        return Ok(lista);
    }

    [HttpPost("{id:int}/ofertas")]
    [Authorize(Roles = SecurityDefaults.TecnicoRole)]
    public async Task<IActionResult> Ofertar(int id, [FromBody] CrearOfertaRequest request)
    {
        var tecnico = await TecnicoActual();
        if (tecnico == null) return Unauthorized();
        if (!PremiumMembership.IsVigente(tecnico, DateTime.UtcNow)) return Forbid();
        if (request.PrecioEstimado is < 0 or > 9_999_999_999.99m || string.IsNullOrWhiteSpace(request.Disponibilidad) ||
            request.Disponibilidad.Trim().Length > 100 || (request.Mensaje?.Trim().Length ?? 0) > 500)
            return BadRequest("La oferta no tiene datos válidos.");

        var urgencia = await _db.Urgencias.FindAsync(id);
        if (urgencia == null) return NotFound();
        var mismaCategoria = tecnico.CategoriaId.HasValue
            ? urgencia.CategoriaId == tecnico.CategoriaId
            : string.Equals(urgencia.Rubro, tecnico.Rubro, StringComparison.OrdinalIgnoreCase);
        if (!mismaCategoria) return Forbid();
        if (urgencia.Estado != UrgenciaEstados.Publicada || urgencia.OfertaSeleccionadaId != null)
            return Conflict("La urgencia ya no admite ofertas.");
        if (await _db.OfertasUrgencia.AnyAsync(o => o.UrgenciaId == id && o.TecnicoId == tecnico.Id))
            return Conflict("Ya enviaste una oferta para esta urgencia.");

        var oferta = new OfertaUrgencia
        {
            UrgenciaId = id,
            TecnicoId = tecnico.Id,
            PrecioEstimado = request.PrecioEstimado,
            Disponibilidad = request.Disponibilidad.Trim(),
            Mensaje = request.Mensaje?.Trim() ?? "",
            FechaCreacion = DateTime.UtcNow
        };
        _db.OfertasUrgencia.Add(oferta);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Conflict("La urgencia ya no admite esta oferta o ya enviaste una.");
        }
        return Ok(oferta);
    }

    [HttpGet("mis-ofertas")]
    [Authorize(Roles = SecurityDefaults.TecnicoRole)]
    public async Task<IActionResult> MisOfertas()
    {
        var tecnico = await TecnicoActual();
        if (tecnico == null) return Unauthorized();
        var lista = await (from o in _db.OfertasUrgencia
                           join u in _db.Urgencias on o.UrgenciaId equals u.Id
                           where o.TecnicoId == tecnico.Id
                           orderby o.FechaCreacion descending
                           select new
                           {
                               o.Id, o.UrgenciaId, o.PrecioEstimado, o.Disponibilidad, o.Mensaje,
                               o.FechaCreacion, u.Rubro, u.Descripcion, u.ZonaBarrio, u.FotoUrl,
                               Seleccionada = u.OfertaSeleccionadaId == o.Id
                           }).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("seleccionadas")]
    [Authorize(Roles = SecurityDefaults.TecnicoRole)]
    public async Task<IActionResult> Seleccionadas()
    {
        var tecnico = await TecnicoActual();
        if (tecnico == null) return Unauthorized();
        var items = await _db.Urgencias
            .Where(u => u.TecnicoId == tecnico.Id && u.OfertaSeleccionadaId != null)
            .OrderByDescending(u => u.FechaSeleccion).ToListAsync();
        return Ok(items.Select(u => new
        {
            u.Id, u.Rubro, u.Descripcion, u.ZonaBarrio, u.FotoUrl, u.Fecha, u.FechaSeleccion,
            DireccionExacta = LeerProtegido(u.DireccionExactaProtegida),
            ContactoCliente = LeerContacto(u)
        }));
    }

    [HttpGet("{id:int}/cliente")]
    [AllowAnonymous]
    public async Task<IActionResult> DetalleCliente(int id)
    {
        var urgencia = await _db.Urgencias.FindAsync(id);
        if (urgencia == null) return NotFound();
        if (!TokenValido(urgencia)) return Unauthorized();

        var ofertas = await (from o in _db.OfertasUrgencia
                             join t in _db.Tecnicos on o.TecnicoId equals t.Id
                             where o.UrgenciaId == id
                             orderby o.FechaCreacion
                             select new OfertaClienteDto
                             {
                                 Id = o.Id, TecnicoId = t.Id, TecnicoNombre = t.Nombre,
                                 TecnicoRubro = t.Rubro, TecnicoLogo = t.Logo,
                                 PrecioEstimado = o.PrecioEstimado, Disponibilidad = o.Disponibilidad,
                                 Mensaje = o.Mensaje, FechaCreacion = o.FechaCreacion,
                                 Seleccionada = urgencia.OfertaSeleccionadaId == o.Id,
                                 PromedioEstrellas = _db.CalificacionesServicio.Where(c => c.TecnicoId == t.Id)
                                     .Select(c => (double?)c.Estrellas).Average() ?? 0,
                                 CantidadOpiniones = _db.CalificacionesServicio.Count(c => c.TecnicoId == t.Id)
                             }).ToListAsync();

        return Ok(new
        {
            urgencia.Id, urgencia.Rubro, urgencia.Descripcion, urgencia.ZonaBarrio,
            urgencia.FotoUrl, urgencia.Estado, urgencia.Fecha, urgencia.OfertaSeleccionadaId,
            DireccionExacta = LeerProtegido(urgencia.DireccionExactaProtegida),
            ContactoCliente = LeerContacto(urgencia), Ofertas = ofertas,
            YaCalificada = await _db.CalificacionesServicio.AnyAsync(c => c.UrgenciaId == id)
        });
    }

    [HttpPost("{id:int}/seleccionar-oferta/{ofertaId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> SeleccionarOferta(int id, int ofertaId)
    {
        var urgencia = await _db.Urgencias.FindAsync(id);
        if (urgencia == null) return NotFound();
        if (!TokenValido(urgencia)) return Unauthorized();
        if (urgencia.OfertaSeleccionadaId != null) return Conflict("Ya seleccionaste una oferta.");

        var oferta = await _db.OfertasUrgencia.FirstOrDefaultAsync(o => o.Id == ofertaId && o.UrgenciaId == id);
        if (oferta == null) return BadRequest("La oferta no pertenece a esta urgencia.");
        var tecnico = await _db.Tecnicos.FindAsync(oferta.TecnicoId);
        if (tecnico == null || !PremiumMembership.IsVigente(tecnico, DateTime.UtcNow))
            return Conflict("El técnico ya no tiene Premium vigente.");

        var actualizadas = await _db.Urgencias
            .Where(u => u.Id == id && u.OfertaSeleccionadaId == null && u.Estado == UrgenciaEstados.Publicada)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.OfertaSeleccionadaId, (int?)oferta.Id)
                .SetProperty(u => u.TecnicoId, (int?)oferta.TecnicoId)
                .SetProperty(u => u.Estado, UrgenciaEstados.Seleccionada)
                .SetProperty(u => u.FechaSeleccion, (DateTime?)DateTime.UtcNow));
        if (actualizadas != 1) return Conflict("Otra oferta ya fue seleccionada.");
        return Ok(new { oferta.Id, oferta.TecnicoId, tecnico.Nombre, tecnico.WhatsApp });
    }

    [HttpPost("{id:int}/calificacion")]
    [AllowAnonymous]
    public async Task<IActionResult> Calificar(int id, [FromBody] CrearCalificacionRequest request)
    {
        if (request.Estrellas is < 1 or > 5 || (request.Comentario?.Trim().Length ?? 0) > 500)
            return BadRequest("La calificación debe tener entre 1 y 5 estrellas.");
        var urgencia = await _db.Urgencias.FindAsync(id);
        if (urgencia == null) return NotFound();
        if (!TokenValido(urgencia)) return Unauthorized();
        if (urgencia.TecnicoId == null || urgencia.OfertaSeleccionadaId == null)
            return Conflict("Primero tenés que seleccionar una oferta.");
        if (await _db.CalificacionesServicio.AnyAsync(c => c.UrgenciaId == id))
            return Conflict("Esta solicitud ya fue calificada.");

        _db.CalificacionesServicio.Add(new CalificacionServicio
        {
            UrgenciaId = id, TecnicoId = urgencia.TecnicoId.Value, Estrellas = request.Estrellas,
            Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim(),
            FechaCreacion = DateTime.UtcNow
        });
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("Esta solicitud ya fue calificada."); }
        return Ok();
    }

    [HttpGet("tecnico/{tecnicoId:int}")]
    [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
    [Obsolete("Usar /disponibles, /mis-ofertas y /seleccionadas.")]
    public async Task<IActionResult> GetAnteriorPorTecnico(int tecnicoId)
    {
        if (!User.CanAccess(SecurityDefaults.TecnicoRole, tecnicoId)) return Forbid();
        var lista = await _db.Urgencias.Where(u => u.TecnicoId == tecnicoId)
            .OrderByDescending(u => u.Fecha).Select(u => new
            { u.Id, u.Rubro, u.Descripcion, u.ZonaBarrio, u.FotoUrl, u.Estado, u.Fecha }).ToListAsync();
        return Ok(lista);
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = $"{SecurityDefaults.AdminRole},{SecurityDefaults.TecnicoRole}")]
    [Obsolete("Los estados libres fueron reemplazados por selección de oferta.")]
    public IActionResult CambiarEstadoAnterior(int id) => StatusCode(StatusCodes.Status410Gone,
        "Los estados de urgencia ya no se modifican manualmente.");

    private async Task<Tecnico?> TecnicoActual()
    {
        var id = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : 0;
        return id > 0 ? await _db.Tecnicos.FindAsync(id) : null;
    }

    private bool TokenValido(SolicitudUrgencia urgencia)
    {
        var token = Request.Headers[ClientTokenHeader].FirstOrDefault();
        return token != null && _tokens.Matches(token, urgencia.TokenClienteHash);
    }

    private string LeerContacto(SolicitudUrgencia urgencia) =>
        !string.IsNullOrWhiteSpace(urgencia.ContactoClienteProtegido)
            ? LeerProtegido(urgencia.ContactoClienteProtegido) : urgencia.WhatsAppCliente;

    private string LeerProtegido(string value) => string.IsNullOrWhiteSpace(value) ? "" : _protector.Unprotect(value);

}

public sealed class PublicarUrgenciaForm
{
    public int CategoriaId { get; set; }
    public string Descripcion { get; set; } = "";
    public string ZonaBarrio { get; set; } = "";
    public string DireccionExacta { get; set; } = "";
    public string ContactoCliente { get; set; } = "";
    public IFormFile? Foto { get; set; }
}

public sealed record PublicarUrgenciaResponse(int Id, string Token, string Estado, DateTime Fecha);
public sealed record CrearOfertaRequest(decimal PrecioEstimado, string Disponibilidad, string? Mensaje);
public sealed record CrearCalificacionRequest(int Estrellas, string? Comentario);

public sealed class OfertaClienteDto
{
    public int Id { get; set; }
    public int TecnicoId { get; set; }
    public string TecnicoNombre { get; set; } = "";
    public string TecnicoRubro { get; set; } = "";
    public string TecnicoLogo { get; set; } = "";
    public decimal PrecioEstimado { get; set; }
    public string Disponibilidad { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public bool Seleccionada { get; set; }
    public double PromedioEstrellas { get; set; }
    public int CantidadOpiniones { get; set; }
}
