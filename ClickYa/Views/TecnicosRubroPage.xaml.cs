using System.Collections.ObjectModel;
using System.Text.Json;

namespace ClickYa.Views;

[QueryProperty(nameof(Rubro), "rubro")]
[QueryProperty(nameof(CategoriaId), "categoriaId")]
public partial class TecnicosRubroPage : ContentPage
{
    private const string BASE_URL = "https://clickya-production.up.railway.app";

    private string _rubro = "";
    private int _categoriaId;
    public string CategoriaId
    {
        set
        {
            if (int.TryParse(value, out var id)) _categoriaId = id;
            if (_categoriaId > 0) CargarTecnicos();
        }
    }
    public string Rubro
    {
        set
        {
            _rubro = value;
            LblRubro.Text = value;
            if (_categoriaId > 0) CargarTecnicos();
        }
    }

    public TecnicosRubroPage()
    {
        InitializeComponent();
    }

    private async void CargarTecnicos()
    {
        try
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync(
                $"{BASE_URL}/api/Tecnico/categoria/{_categoriaId}");

            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lista = JsonSerializer.Deserialize<List<TecnicoItem>>(json, opciones) ?? new();

            Location? ubicacionUsuario = null;
            try
            {
                ubicacionUsuario = await Geolocation.GetLastKnownLocationAsync()
                    ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low));
            }
            catch { }

            var ordenada = lista
                .Select(t => new
                {
                    Tecnico = t,
                    Km = (ubicacionUsuario != null && t.Latitud != 0 && t.Longitud != 0)
                        ? Location.CalculateDistance(ubicacionUsuario.Latitude, ubicacionUsuario.Longitude, t.Latitud, t.Longitud, DistanceUnits.Kilometers)
                        : double.MaxValue
                })
                .OrderByDescending(x => x.Tecnico.PremiumVigente)
                .ThenBy(x => x.Km)
                .Select(x => x.Tecnico)
                .ToList();

            ListaTecnicos.ItemsSource = ordenada;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error cargando técnicos: " + ex.Message);
        }
    }

    private async void OnVolverTapped(object sender, EventArgs e)
        => await Navigation.PopAsync();

    private async void OnTecnicoTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not TecnicoItem tecnico) return;
        await Shell.Current.GoToAsync(
            $"perfil-tecnico?id={tecnico.Id}");
    }
}
public class TecnicoItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Rubro { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string FotoPortada { get; set; } = "";
    public string Logo { get; set; } = "";
    public bool EsPremium { get; set; }
    public bool PremiumVigente { get; set; }
    public bool Activo { get; set; }
    public double Latitud { get; set; } = 0;
    public double Longitud { get; set; } = 0;
    public double PromedioEstrellas { get; set; }
    public int CantidadOpiniones { get; set; }
    public string ReputacionTexto => CantidadOpiniones == 0
        ? "Sin opiniones"
        : $"⭐ {PromedioEstrellas:F1} ({CantidadOpiniones})";

    public string Iniciales => Nombre.Length >= 2
        ? Nombre.Substring(0, 2).ToUpper()
        : Nombre.ToUpper();

    public bool TieneDireccion => !string.IsNullOrEmpty(Direccion);
    public string DireccionCompleta => string.IsNullOrEmpty(Direccion) ? "" : "📍 " + Direccion;
    public string LogoUrl => string.IsNullOrEmpty(Logo)
    ? "" : $"https://clickya-production.up.railway.app{Logo}";
    public bool TieneLogo => !string.IsNullOrEmpty(Logo);
    public bool SinLogo => string.IsNullOrEmpty(Logo);
}
