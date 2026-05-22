using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClickYa.Models;

namespace ClickYa.Services
{
    public class ClickYaDataService
    {
        private readonly string baseUrl = "http://192.168.100.9:5191";

        public string BaseUrl => baseUrl;

        // Settings para que funcione con mayúsculas o minúsculas
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        // =========================================
        // OBTENER UN SOLO COMERCIO POR ID
        // =========================================
        public async Task<Local?> ObtenerLocalAsync(int id)
        {
            try
            {
                using var client = new HttpClient();
                var url = $"{baseUrl}/api/comercio/{id}";
                var json = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<Local>(json, _settings);
            }
            catch
            {
                return null;
            }
        }

        // =========================================
        // OBTENER PUBLICACIONES POR COMERCIO
        // =========================================
        public async Task<List<Publicacion>?> ObtenerPublicacionesAsync(int comercioId)
        {
            try
            {
                using var client = new HttpClient();
                var url = $"{baseUrl}/api/Publicaciones/comercio/{comercioId}";
                var json = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<List<Publicacion>>(json, _settings);
            }
            catch
            {
                return null;
            }
        }

        // =========================================
        // OBTENER TODOS LOS COMERCIOS
        // =========================================
        public async Task<List<Local>?> ObtenerComerciosAsync()
        {
            try
            {
                using var client = new HttpClient();
                var url = $"{baseUrl}/api/Comercio/todos";
                var json = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<List<Local>>(json, _settings);
            }
            catch
            {
                return null;
            }
        }
    }
}