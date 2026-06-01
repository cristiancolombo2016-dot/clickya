using ClickYa.Models;
using ClickYa.Services;
using System.Collections.ObjectModel;

namespace ClickYa.Views
{
    [QueryProperty(nameof(Categoria), "categoria")]
    public partial class TiendasListaPage : ContentPage
    {
        private readonly ComerciosService _comercioService = new();
        private readonly string _baseUrl = "https://clickya-production.up.railway.app";
        private Location? _ubicacionUsuario;
        public ObservableCollection<TiendaItemVM> Tiendas { get; } = new();
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

        public TiendasListaPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private async Task IniciarConUbicacion()
        {
            await ObtenerUbicacionUsuario();
            await CargarTiendasAsync();
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

        private string CalcularDistancia(double latLocal, double lngLocal)
        {
            if (_ubicacionUsuario == null || latLocal == 0 || lngLocal == 0)
                return "";

            var distancia = Location.CalculateDistance(
                _ubicacionUsuario.Latitude, _ubicacionUsuario.Longitude,
                latLocal, lngLocal,
                DistanceUnits.Kilometers);

            return distancia < 1
                ? $"{(int)(distancia * 1000)} m"
                : $"{distancia:F1} km";
        }

        private async Task CargarTiendasAsync()
        {
            try
            {
                Tiendas.Clear();
                LblVacio.IsVisible = false;

                var locales = await _comercioService.ObtenerPorRubroAsync("tiendas");
                if (locales == null || locales.Count == 0)
                {
                    LblVacio.IsVisible = true;
                    return;
                }

                var cat = (_categoria ?? "").Trim().ToLower();

                var tiendas = locales
                    .Where(l => (l.categoria ?? "").ToLower() == cat)
                    .ToList();

                foreach (var t in tiendas)
                {
                    Tiendas.Add(new TiendaItemVM
                    {
                        Local = t,
                        LogoFinal = string.IsNullOrWhiteSpace(t.logoUrl)
                            ? "logo_moda.png"
                            : (t.logoUrl.StartsWith("http")
                                ? t.logoUrl
                                : $"{_baseUrl}{t.logoUrl}"),
                        distancia = (t.latitud == 0 || t.longitud == 0)
    ? (string.IsNullOrWhiteSpace(t.ubicacion) ? "Sin ubicación" : t.ubicacion)
    : CalcularDistancia(t.latitud, t.longitud)
                    });
                }

                if (Tiendas.Count == 0)
                    LblVacio.IsVisible = true;
            }
            catch
            {
                LblVacio.IsVisible = true;
            }
        }

        private async void AbrirLocal_Tapped(object sender, TappedEventArgs e)
        {
            if (e?.Parameter is not TiendaItemVM vm || vm.Local == null)
                return;

            System.Diagnostics.Debug.WriteLine($"NAVEGANDO A LOCAL ID: {vm.Local.id}");

            await Shell.Current.GoToAsync($"tienda-local?id={vm.Local.id}");
        }
    }

    public class TiendaItemVM
    {
        public Local? Local { get; set; }
        public string LogoFinal { get; set; } = "logo_moda.png";
        public string nombre => Local?.nombre ?? "";
        public string ubicacion => Local?.ubicacion ?? "";
        public string distancia { get; set; } = "";
        public string direccion => Local?.ubicacion ?? "";
        public string separador => (!string.IsNullOrWhiteSpace(Local?.ubicacion) && !string.IsNullOrWhiteSpace(distancia) && distancia != Local?.ubicacion) ? " · " : "";
        public string kmTexto => distancia == Local?.ubicacion ? "" : distancia;
    }
}