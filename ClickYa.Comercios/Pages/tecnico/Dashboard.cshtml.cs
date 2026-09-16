using ClickYa.Comercios.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ClickYa.Comercios.Pages.tecnico
{
    public class DashboardModel : PageModel
    {
        private readonly IHttpClientFactory _clients;

        public DashboardModel(IHttpClientFactory clients) => _clients = clients;

        public int TecnicoId { get; private set; }
        public string Error { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(string? ticket, string? token)
        {
            if (User.IsInRole(WebSecurity.TecnicoRole) &&
                int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) && currentId > 0)
            {
                TecnicoId = currentId;
                return Page();
            }

            await HttpContext.SignOutAsync(WebSecurity.CookieScheme);
            ApiAccess? access = null;
            try
            {
                HttpResponseMessage? response = null;
                if (!string.IsNullOrWhiteSpace(ticket))
                    response = await _clients.CreateClient("ClickYaApi").PostAsJsonAsync(
                        "api/Auth/ticket",
                        new { ticket });
                else if (!string.IsNullOrWhiteSpace(token))
                {
                    response = await _clients.CreateClient("ClickYaApi").PostAsJsonAsync(
                        "api/Auth/legacy-token",
                        new { role = WebSecurity.TecnicoRole, token });
                }
                if (response?.IsSuccessStatusCode == true)
                    access = await response.Content.ReadFromJsonAsync<ApiAccess>();
            }
            catch (HttpRequestException)
            {
                Error = "No se pudo validar el acceso.";
            }

            if (access?.Role != WebSecurity.TecnicoRole || access.SubjectId is not > 0)
            {
                Error = string.IsNullOrEmpty(Error) ? "El enlace de acceso no es válido." : Error;
                return RedirectToPage("/Index");
            }

            await WebSecurity.SignInAsync(HttpContext, access);
            return RedirectToPage("/tecnico/Dashboard");
        }
    }
}
