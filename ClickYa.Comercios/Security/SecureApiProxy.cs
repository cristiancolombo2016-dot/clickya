using System.Net.Http.Headers;

namespace ClickYa.Comercios.Security;

public static class SecureApiProxy
{
    public static async Task Forward(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var accessToken = context.User.FindFirst(WebSecurity.ApiTokenClaim)?.Value;
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var apiBaseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var path = context.Request.Path.Value?["/secure-api".Length..] ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        var target = new Uri(new Uri(apiBaseUrl.TrimEnd('/') + "/"), path.TrimStart('/') + context.Request.QueryString);
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (context.Request.Headers.TryGetValue("Accept", out var accept))
            request.Headers.TryAddWithoutValidation("Accept", accept.ToArray());

        if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
        }

        var clients = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clients.CreateClient("ClickYaApi")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        if (response.Content.Headers.ContentType != null)
            context.Response.ContentType = response.Content.Headers.ContentType.ToString();
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
