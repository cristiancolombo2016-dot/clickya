using ClickYa.Comercios.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace ClickYa.Comercios.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _clients;

        public LoginModel(IHttpClientFactory clients) => _clients = clients;

        [BindProperty]
        public string Usuario { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string Error { get; set; } = "";

        public IActionResult OnGet()
        {
            return User.IsInRole(WebSecurity.AdminRole)
                ? RedirectToPage("/Admin/Index")
                : Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var response = await _clients.CreateClient("ClickYaApi").PostAsJsonAsync(
                    "api/Auth/admin/login",
                    new { username = Usuario, password = Password });
                if (response.IsSuccessStatusCode)
                {
                    var access = await response.Content.ReadFromJsonAsync<ApiAccess>();
                    if (access != null)
                    {
                        await WebSecurity.SignInAsync(HttpContext, access);
                        return RedirectToPage("/Admin/Index");
                    }
                }
            }
            catch (HttpRequestException)
            {
                Error = "No se pudo conectar con el servicio de autenticación.";
                return Page();
            }

            Error = "Usuario o contraseña incorrectos";
            return Page();
        }
    }
}
