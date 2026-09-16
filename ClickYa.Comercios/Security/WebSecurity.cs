using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ClickYa.Comercios.Security;

public static class WebSecurity
{
    public const string CookieScheme = "ClickYaPanel";
    public const string ApiTokenClaim = "clickya:api_token";
    public const string AdminRole = "Admin";
    public const string ComercioRole = "Comercio";
    public const string TecnicoRole = "Tecnico";

    public static async Task SignInAsync(HttpContext context, ApiAccess response)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken) || string.IsNullOrWhiteSpace(response.Role))
            throw new InvalidOperationException("La API no devolvió una identidad válida.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, (response.SubjectId ?? 0).ToString()),
            new(ClaimTypes.Name, response.Name ?? response.Role),
            new(ClaimTypes.Role, response.Role),
            new(ApiTokenClaim, response.AccessToken)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieScheme));
        await context.SignInAsync(
            CookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(55)
            });
    }
}

public sealed record ApiAccess(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("subjectId")] int? SubjectId,
    [property: JsonPropertyName("name")] string? Name);
