using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace ClickYa.Comercios.Pages.Admin
{
    [IgnoreAntiforgeryToken]
    public class ComerciosModel : PageModel
    {
        private readonly HttpClient _http;

        public List<SolicitudComercio> Solicitudes { get; set; } = new();

        public ComerciosModel(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient();
            _http.BaseAddress = new Uri("http://192.168.100.9:5191/");
        }

        public async Task OnGetAsync()
        {
            var data = await _http.GetFromJsonAsync<List<SolicitudComercio>>("api/solicitudes");

            if (data != null)
            {
                Solicitudes = data;
            }
        }

        public async Task<IActionResult> OnPostAprobarAsync(int id)
        {
            var response = await _http.PutAsync(
                $"api/solicitudes/{id}/aprobar",
                null
            );
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _http.DeleteAsync($"api/solicitudes/{id}");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBloquearAsync(int id)
        {
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
        public string? Token { get; set; }
        public int ComercioId { get; set; }
        public bool EsDestacado { get; set; } = false;
    }
}
