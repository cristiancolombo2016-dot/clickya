using ClickYa.Comercios.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ClickYa.Comercios.Pages.Comercio
{
    public class DashboardModel : PageModel
    {
        private readonly IHttpClientFactory _clients;

        public DashboardModel(IHttpClientFactory clients) => _clients = clients;

        public int ComercioId { get; private set; }
        public string Error { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(string? ticket, string? token)
        {
            if (User.IsInRole(WebSecurity.ComercioRole) &&
                int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) && currentId > 0)
            {
                ComercioId = currentId;
                return Page();
            }

            await HttpContext.SignOutAsync(WebSecurity.CookieScheme);
            ApiAccess? access = null;
            try
            {
                var client = _clients.CreateClient("ClickYaApi");
                HttpResponseMessage? response = null;
                if (!string.IsNullOrWhiteSpace(ticket))
                    response = await client.PostAsJsonAsync("api/Auth/ticket", new { ticket });
                else if (!string.IsNullOrWhiteSpace(token))
                    response = await client.PostAsJsonAsync("api/Auth/legacy-token", new { role = WebSecurity.ComercioRole, token });

                if (response?.IsSuccessStatusCode == true)
                    access = await response.Content.ReadFromJsonAsync<ApiAccess>();
            }
            catch (HttpRequestException)
            {
                Error = "No se pudo validar el acceso.";
            }

            if (access?.Role != WebSecurity.ComercioRole || access.SubjectId is not > 0)
            {
                Error = string.IsNullOrEmpty(Error) ? "El enlace de acceso no es válido o venció." : Error;
                return RedirectToPage("/Index");
            }

            await WebSecurity.SignInAsync(HttpContext, access);
            return RedirectToPage("/Comercio/Dashboard");
        }
    }
}
