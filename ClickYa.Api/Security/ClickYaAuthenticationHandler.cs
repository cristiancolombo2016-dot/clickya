using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClickYa.Api.Security;

public sealed class ClickYaAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AccessTokenService _tokens;

    public ClickYaAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AccessTokenService tokens) : base(options, logger, encoder)
    {
        _tokens = tokens;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
            return Task.FromResult(AuthenticateResult.NoResult());

        var principal = _tokens.Validate(header.Parameter);
        if (principal == null)
            return Task.FromResult(AuthenticateResult.Fail("Token inválido o vencido."));

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SecurityDefaults.AuthenticationScheme)));
    }
}
