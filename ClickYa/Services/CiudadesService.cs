using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClickYa.Services
{
    public class CiudadesService
    {
        // LOCAL (API que acabás de crear)
        private readonly string apiUrl = "https://clickya-production.up.railway.app/api/ciudades";


        // ANDROID EMULATOR: usar http://10.0.2.2:5191/api/ciudades

        public async Task<List<Ciudad>?> ObtenerCiudadesAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync(apiUrl);

                var ciudades = JsonConvert.DeserializeObject<List<Ciudad>>(response);
                return ciudades;
            }
            catch
            {
                return null;
            }
        }
    }

    public class Ciudad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public bool Activa { get; set; }
    }
}
