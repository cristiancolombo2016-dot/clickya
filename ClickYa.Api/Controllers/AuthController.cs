using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClickYa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AccessTokenService _tokens;
    private readonly WebLoginTicketService _tickets;
    private readonly LoginThrottle _throttle;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext db,
        AccessTokenService tokens,
        WebLoginTicketService tickets,
        LoginThrottle throttle,
        IConfiguration configuration)
    {
        _db = db;
        _tokens = tokens;
        _tickets = tickets;
        _throttle = throttle;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("admin/login")]
    public IActionResult AdminLogin([FromBody] AdminLoginRequest request)
    {
        var throttleKey = $"admin:{HttpContext.Connection.RemoteIpAddress}";
        if (_throttle.IsBlocked(throttleKey))
            return StatusCode(StatusCodes.Status429TooManyRequests, "Demasiados intentos. Probá nuevamente más tarde.");

        var expectedUser = _configuration["Security:AdminUsername"];
        var passwordHash = _configuration["Security:AdminPasswordHash"];
        var validUser = !string.IsNullOrWhiteSpace(expectedUser) &&
                        string.Equals(request.Username, expectedUser, StringComparison.Ordinal);
        var validPassword = !string.IsNullOrWhiteSpace(passwordHash) &&
                            PasswordSecurity.Verify(request.Password, passwordHash, out _) &&
                            PasswordSecurity.IsHash(passwordHash);

        if (!validUser || !validPassword)
        {
            _throttle.RegisterFailure(throttleKey);
            return Unauthorized("Credenciales inválidas.");
        }

        _throttle.RegisterSuccess(throttleKey);
        return Ok(new AccessTokenResponse(
            _tokens.Create(SecurityDefaults.AdminRole, 0, expectedUser!),
            SecurityDefaults.AdminRole));
    }

    [AllowAnonymous]
    [HttpPost("legacy-token")]
    public async Task<IActionResult> LegacyToken([FromBody] LegacyTokenRequest request)
    {
        var throttleKey = $"legacy:{HttpContext.Connection.RemoteIpAddress}";
        if (_throttle.IsBlocked(throttleKey))
            return StatusCode(StatusCodes.Status429TooManyRequests, "Demasiados intentos. Probá nuevamente más tarde.");

        if (string.IsNullOrWhiteSpace(request.Token))
            return Unauthorized("Acceso inválido.");

        AccessTokenResponse? response = request.Role switch
        {
            SecurityDefaults.ComercioRole => await AuthenticateComercioToken(request.Token),
            SecurityDefaults.TecnicoRole => await AuthenticateTecnicoToken(request.Token),
            _ => null
        };

        if (response == null)
        {
            _throttle.RegisterFailure(throttleKey);
            return Unauthorized("Acceso inválido.");
        }

        _throttle.RegisterSuccess(throttleKey);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("ticket")]
    public IActionResult ExchangeTicket([FromBody] WebTicketRequest request)
    {
        var accessToken = _tickets.Redeem(request.Ticket ?? "");
        if (accessToken == null)
            return Unauthorized("El acceso venció o ya fue utilizado.");

        var principal = _tokens.Validate(accessToken);
        var role = principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var subjectValue = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var name = principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (role == null || !int.TryParse(subjectValue, out var subjectId))
            return Unauthorized("El acceso no es válido.");

        return Ok(new AccessTokenResponse(accessToken, role, subjectId, name));
    }

    private async Task<AccessTokenResponse?> AuthenticateComercioToken(string suppliedToken)
    {
        var comercios = await _db.Comercios
            .Where(x => x.Estado == "Activo" && x.Token != "")
            .ToListAsync();
        var comercio = comercios.FirstOrDefault(x =>
            PasswordSecurity.Verify(suppliedToken, x.Token, out _));
        if (comercio == null) return null;

        if (!PasswordSecurity.IsHash(comercio.Token))
        {
            comercio.Token = PasswordSecurity.Hash(suppliedToken);
            var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(x => x.ComercioId == comercio.Id);
            if (solicitud != null) solicitud.Token = comercio.Token;
            await _db.SaveChangesAsync();
        }

        return new AccessTokenResponse(
            _tokens.Create(SecurityDefaults.ComercioRole, comercio.Id, comercio.Nombre),
            SecurityDefaults.ComercioRole,
            comercio.Id,
            comercio.Nombre);
    }

    private async Task<AccessTokenResponse?> AuthenticateTecnicoToken(string suppliedToken)
    {
        var tecnicos = await _db.Tecnicos
            .Where(x => x.Activo && x.Token != "")
            .ToListAsync();
        var tecnico = tecnicos.FirstOrDefault(x =>
            PasswordSecurity.Verify(suppliedToken, x.Token, out _));
        if (tecnico == null) return null;

        if (!PasswordSecurity.IsHash(tecnico.Token))
        {
            tecnico.Token = PasswordSecurity.Hash(suppliedToken);
            await _db.SaveChangesAsync();
        }

        return new AccessTokenResponse(
            _tokens.Create(SecurityDefaults.TecnicoRole, tecnico.Id, tecnico.Nombre),
            SecurityDefaults.TecnicoRole,
            tecnico.Id,
            tecnico.Nombre);
    }
}

public sealed record AdminLoginRequest(string Username, string Password);
public sealed record LegacyTokenRequest(string Role, string Token);
public sealed record WebTicketRequest(string? Ticket);
public sealed record AccessTokenResponse(
    string AccessToken,
    string Role,
    int? SubjectId = null,
    string? Name = null);
