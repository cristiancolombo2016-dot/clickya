using Microsoft.AspNetCore.Mvc;

namespace ClickYa.Api.Controllers;

[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private static readonly List<SolicitudComercio> solicitudes = new();
    private static readonly List<Local> comercios = new();

    [HttpPost]
    public IActionResult Crear([FromBody] SolicitudComercio solicitud)
    {
        try
        {
            var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
            if (!System.IO.File.Exists(ruta)) System.IO.File.WriteAllText(ruta, "[]");

            var json = System.IO.File.ReadAllText(ruta);
            var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json)
                        ?? new List<SolicitudComercio>();

            // Verificar email duplicado
            if (!string.IsNullOrWhiteSpace(solicitud.Email) &&
                lista.Any(s => s.Email?.ToLower() == solicitud.Email.ToLower()))
                return BadRequest("Ya existe un comercio con ese email");

            solicitud.Id = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1;
            solicitud.CreatedAt = DateTime.Now;

            // AUTO APROBAR — crear comercio y token automáticamente
            var token = Guid.NewGuid().ToString("N");
            solicitud.Token = token;

            var rutaComercios = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "comercios.json");
            if (!System.IO.File.Exists(rutaComercios)) System.IO.File.WriteAllText(rutaComercios, "[]");

            var jsonComercios = System.IO.File.ReadAllText(rutaComercios);
            var listaComercios = System.Text.Json.JsonSerializer.Deserialize<List<ClickYa.Api.Models.Comercio>>(jsonComercios)
                                ?? new List<ClickYa.Api.Models.Comercio>();

            var nuevoComercio = new ClickYa.Api.Models.Comercio
            {
                Id = listaComercios.Count == 0 ? 1 : listaComercios.Max(x => x.Id) + 1,
                Nombre = solicitud.Nombre,
                Rubro = solicitud.Rubro,
                Categoria = solicitud.Categoria,
                Descripcion = solicitud.Descripcion,
                WhatsApp = solicitud.Telefono,
                Ubicacion = "San Nicolás",
                Estado = "Activo",
                Token = token
            };

            solicitud.ComercioId = nuevoComercio.Id;
            solicitud.Estado = "CONFIABLE";

            listaComercios.Add(nuevoComercio);

            var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(rutaComercios, System.Text.Json.JsonSerializer.Serialize(listaComercios, opts));

            lista.Add(solicitud);
            System.IO.File.WriteAllText(ruta, System.Text.Json.JsonSerializer.Serialize(lista, opts));

            return Ok(new { token = token, comercioId = nuevoComercio.Id, nombre = solicitud.Nombre });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
        if (!System.IO.File.Exists(ruta)) return Unauthorized("Email o contraseña incorrectos");

        var json = System.IO.File.ReadAllText(ruta);
        var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json) ?? new();

        var solicitud = lista.FirstOrDefault(s =>
            s.Email?.ToLower() == req.Email?.ToLower() &&
            s.Password == req.Password &&
            s.Estado == "CONFIABLE");

        if (solicitud == null) return Unauthorized("Email o contraseña incorrectos");

        return Ok(new { token = solicitud.Token, comercioId = solicitud.ComercioId, nombre = solicitud.Nombre });
    }

    [HttpGet]
    public IActionResult Listar()
    {
        var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
        if (!System.IO.File.Exists(ruta)) return Ok(new List<SolicitudComercio>());
        var json = System.IO.File.ReadAllText(ruta);
        var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json) ?? new();
        return Ok(lista);
    }

    [HttpPut("{id}/aprobar")]
    public IActionResult Aprobar(int id)
    {
        var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
        if (!System.IO.File.Exists(ruta)) return NotFound();
        var json = System.IO.File.ReadAllText(ruta);
        var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json) ?? new();
        var solicitud = lista.FirstOrDefault(s => s.Id == id);
        if (solicitud == null) return NotFound();
        solicitud.Estado = "CONFIABLE";
        var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        System.IO.File.WriteAllText(ruta, System.Text.Json.JsonSerializer.Serialize(lista, opts));
        return Ok(new { token = solicitud.Token, comercioId = solicitud.ComercioId });
    }

    [HttpGet("/api/comercios")]
    public IActionResult ObtenerComercios() => Ok(comercios);

    [HttpGet("token/{token}")]
    public IActionResult GetPorToken(string token)
    {
        var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
        if (!System.IO.File.Exists(ruta)) return NotFound();
        var json = System.IO.File.ReadAllText(ruta);
        var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json) ?? new();
        var solicitud = lista.FirstOrDefault(s => s.Token == token);
        if (solicitud == null) return NotFound("Token inválido");
        return Ok(new { comercioId = solicitud.ComercioId });
    }

    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
        if (!System.IO.File.Exists(ruta)) return NotFound();
        var json = System.IO.File.ReadAllText(ruta);
        var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json) ?? new();
        var solicitud = lista.FirstOrDefault(s => s.Id == id);
        if (solicitud == null) return NotFound();
        lista.Remove(solicitud);
        System.IO.File.WriteAllText(ruta, System.Text.Json.JsonSerializer.Serialize(lista,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        return Ok();
    }

    [HttpPut("{id}/bloquear")]
    public IActionResult Bloquear(int id)
    {
        var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "solicitudes.json");
        if (!System.IO.File.Exists(ruta)) return NotFound();
        var json = System.IO.File.ReadAllText(ruta);
        var lista = System.Text.Json.JsonSerializer.Deserialize<List<SolicitudComercio>>(json) ?? new();
        var solicitud = lista.FirstOrDefault(s => s.Id == id);
        if (solicitud == null) return NotFound();
        solicitud.Estado = "BLOQUEADO";
        System.IO.File.WriteAllText(ruta, System.Text.Json.JsonSerializer.Serialize(lista,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        return Ok();
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class SolicitudComercio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Rubro { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Estado { get; set; } = "NUEVO";
    public DateTime CreatedAt { get; set; }
    public string? LogoUrl { get; set; }
    public string? PortadaUrl { get; set; }
    public string? Token { get; set; }
    public int ComercioId { get; set; }
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EsDestacado { get; set; } = false;
}

public class Local
{
    public int id { get; set; }
    public string nombre { get; set; } = "";
    public string rubro { get; set; } = "";
    public string categoria { get; set; } = "";
    public string descripcion { get; set; } = "";
    public string whatsApp { get; set; } = "";
    public string instagram { get; set; } = "";
    public string ubicacion { get; set; } = "";
    public string correo { get; set; } = "";
    public string horarios { get; set; } = "";
    public string portadaUrl { get; set; } = "";
    public string portadaTipo { get; set; } = "";
    public string logoUrl { get; set; } = "";
    public string estado { get; set; } = "";
}