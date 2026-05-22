using ClickYa.Models;
using ClickYa.Services;
using System.Collections.ObjectModel;

namespace ClickYa.Views
{
    [QueryProperty(nameof(Categoria), "categoria")]
    public partial class ComidaListaPage : ContentPage
    {
        private readonly ComerciosService _comercioService = new();
        private readonly string _baseUrl = "http://192.168.100.9:5191";
        private Location? _ubicacionUsuario;
        public ObservableCollection<ComidaItemVM> Locales { get; } = new();
        private string _categoria = "";

        public string Categoria
        {
            get => _categoria;
            set
            {
                _categoria = value ?? "";
                LblCategoria.Text = _categoria;
                _ = IniciarConUbicacion();
            }
        }

        public ComidaListaPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private async Task IniciarConUbicacion()
        {
            await ObtenerUbicacionUsuario();
            await CargarLocalesAsync();
        }

        private async Task ObtenerUbicacionUsuario()
        {
            try
            {
                var ubicacion = await Geolocation.GetLastKnownLocationAsync();
                if (ubicacion == null)
                    ubicacion = await Geolocation.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Low));
                _ubicacionUsuario = ubicacion;
            }
            catch { }
        }

        private double CalcularDistanciaKm(double lat, double lng)
        {
            if (_ubicacionUsuario == null || lat == 0 || lng == 0) return double.MaxValue;
            return Location.CalculateDistance(
                _ubicacionUsuario.Latitude, _ubicacionUsuario.Longitude,
                lat, lng, DistanceUnits.Kilometers);
        }

        private string FormatearDistancia(double km)
        {
            if (km == double.MaxValue) return "";
            return km < 1 ? $"{(int)(km * 1000)} m" : $"{km:F1} km";
        }

        private async Task CargarLocalesAsync()
        {
            try
            {
                Locales.Clear();
                LblVacio.IsVisible = false;

                List<Local> todos = new();
                try { var l1 = await _comercioService.ObtenerPorRubroAsync("comidas"); if (l1 != null) todos.AddRange(l1); } catch { }
                try { var l2 = await _comercioService.ObtenerPorRubroAsync("comida"); if (l2 != null) todos.AddRange(l2); } catch { }

                var cat = (_categoria ?? "").Trim().ToLower()
                    .Replace("í", "i").Replace("é", "e").Replace("á", "a")
                    .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");

                var filtrados = todos
                    .Where(l =>
                    {
                        var catNorm = (l.categoria ?? "").ToLower()
                            .Replace("í", "i").Replace("é", "e").Replace("á", "a")
                            .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
                        return catNorm.Contains(cat) || cat.Contains(catNorm);
                    })
                    .Select(l => new
                    {
                        Local = l,
                        Km = CalcularDistanciaKm(l.latitud, l.longitud)
                    })
                    .OrderBy(x => x.Km)
                    .ToList();

                foreach (var x in filtrados)
                {
                    Locales.Add(new ComidaItemVM
                    {
                        Local = x.Local,
                        LogoFinal = string.IsNullOrWhiteSpace(x.Local.logoUrl)
                            ? "logo_clickya.png"
                            : (x.Local.logoUrl.StartsWith("http")
                                ? x.Local.logoUrl
                                : $"{_baseUrl}{x.Local.logoUrl}"),
                        distancia = x.Km == double.MaxValue
    ? (string.IsNullOrWhiteSpace(x.Local.ubicacion) ? "Sin ubicación" : x.Local.ubicacion)
    : FormatearDistancia(x.Km)
                    });
                }

                if (Locales.Count == 0)
                    LblVacio.IsVisible = true;
            }
            catch
            {
                LblVacio.IsVisible = true;
            }
        }

        private async void AbrirLocal_Tapped(object sender, TappedEventArgs e)
        {
            if (e?.Parameter is not ComidaItemVM vm || vm.Local == null) return;
            await Shell.Current.GoToAsync($"local?id={vm.Local.id}");
        }
    }

    public class ComidaItemVM
    {
        public Local? Local { get; set; }
        public string LogoFinal { get; set; } = "logo_clickya.png";
        public string nombre => Local?.nombre ?? "";
        public string distancia { get; set; } = "";
        public string direccion => Local?.ubicacion ?? "";
        public string separador => (!string.IsNullOrWhiteSpace(Local?.ubicacion) && !string.IsNullOrWhiteSpace(kmTexto)) ? " · " : "";
        public string kmTexto => distancia == Local?.ubicacion ? "" : distancia;
        public bool tieneKm => !string.IsNullOrWhiteSpace(kmTexto);
    }
}