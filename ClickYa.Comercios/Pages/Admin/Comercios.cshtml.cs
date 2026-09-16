using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ClickYa.Comercios.Security;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;

namespace ClickYa.Comercios.Pages.Admin
{
    public class ComerciosModel : PageModel
    {
        private readonly HttpClient _http;

        public List<SolicitudComercio> Solicitudes { get; set; } = new();

        public ComerciosModel(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("ClickYaApi");
        }

        private void AuthorizeApi() => _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", User.FindFirst(WebSecurity.ApiTokenClaim)?.Value);

        public async Task OnGetAsync()
        {
            AuthorizeApi();
            var data = await _http.GetFromJsonAsync<List<SolicitudComercio>>("api/solicitudes");

            if (data != null)
            {
                Solicitudes = data;
            }
        }

        public async Task<IActionResult> OnPostAprobarAsync(int id)
        {
            AuthorizeApi();
            var response = await _http.PutAsync(
                $"api/solicitudes/{id}/aprobar",
                null
            );
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            AuthorizeApi();
            await _http.DeleteAsync($"api/solicitudes/{id}");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBloquearAsync(int id)
        {
            AuthorizeApi();
            await _http.PutAsync($"api/solicitudes/{id}/bloquear", null);
            return RedirectToPage();
        }
    }

    public class SolicitudComercio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Rubro { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int ComercioId { get; set; }
        public bool EsDestacado { get; set; } = false;
    }
}
