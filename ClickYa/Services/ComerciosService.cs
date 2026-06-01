using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ClickYa.Models;

namespace ClickYa.Services
{
    public class ComerciosService
    {
        private readonly HttpClient _http;

        public ComerciosService()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri("https://clickya-production.up.railway.app/")
            };
        }

        // ============================
        // ENVIAR SOLICITUD DE COMERCIO
        // ============================
        public async Task<bool> EnviarSolicitudAsync(
            string nombre,
            string rubro,
            string telefono,
            string descripcion)
        {
            var solicitud = new
            {
                Nombre = nombre,
                Rubro = rubro,
                Telefono = telefono,
                Descripcion = descripcion
            };

            var response = await _http.PostAsJsonAsync(
                "api/solicitudes",
                solicitud
            );

            return response.IsSuccessStatusCode;
        }

        // ============================
        // OBTENER TODOS LOS COMERCIOS
        // ============================
        public async Task<List<Local>?> ObtenerTodosAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Local>>(
                    "api/Comercio/todos"
                );
            }
            catch
            {
                return null;
            }
        }

        // ============================
        // OBTENER COMERCIOS POR RUBRO
        // ============================
        public async Task<List<Local>?> ObtenerPorRubroAsync(string rubro)
        {
            var response = await _http.GetAsync($"api/Comercio/rubro/{rubro}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}");

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("La API respondió vacío");

            var lista = System.Text.Json.JsonSerializer.Deserialize<List<Local>>(json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (lista == null)
                throw new Exception("No se pudo deserializar la lista");

            return lista;
        }
    }
}