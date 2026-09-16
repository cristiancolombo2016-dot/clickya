using System.Text.Json;

namespace ClickYa.Services;

public sealed class SolicitudLocal
{
    public int Id { get; set; }
    public string Token { get; set; } = "";
    public string Rubro { get; set; } = "";
    public DateTime Fecha { get; set; }
}

public static class SolicitudesLocalesStore
{
    private const string StorageKey = "clickya_urgencias_propias_v1";

    public static async Task<List<SolicitudLocal>> Obtener()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            return string.IsNullOrWhiteSpace(json)
                ? new List<SolicitudLocal>()
                : JsonSerializer.Deserialize<List<SolicitudLocal>>(json) ?? new List<SolicitudLocal>();
        }
        catch
        {
            return new List<SolicitudLocal>();
        }
    }

    public static async Task Guardar(SolicitudLocal solicitud)
    {
        var json = await SecureStorage.Default.GetAsync(StorageKey);
        var actuales = string.IsNullOrWhiteSpace(json)
            ? new List<SolicitudLocal>()
            : JsonSerializer.Deserialize<List<SolicitudLocal>>(json) ?? new List<SolicitudLocal>();
        actuales.RemoveAll(s => s.Id == solicitud.Id);
        actuales.Add(solicitud);
        await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(actuales));
    }
}
