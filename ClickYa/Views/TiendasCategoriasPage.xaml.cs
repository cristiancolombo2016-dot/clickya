using ClickYa.Models;
using ClickYa.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ClickYa.Views
{
    public partial class TiendasCategoriasPage : ContentPage
    {
        private System.Timers.Timer bannerTimer;
        private readonly ComerciosService _comercioService = new();
        private const string BASE_URL = "https://clickya-production.up.railway.app";
        private Location? _ubicacionUsuario;
        private List<Local> _todasTiendas = new();

        public TiendasCategoriasPage()
        {
            InitializeComponent();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsEnabled = true });
            _ = IniciarConUbicacion();
            IniciarCarrusel();
        }

        private async Task IniciarConUbicacion()
        {
            await ObtenerUbicacion();
            await CargarDesdeApi();
        }

        private async Task ObtenerUbicacion()
        {
            try
            {
                var ubicacion = await Geolocation.GetLastKnownLocationAsync();
                if (ubicacion == null)
                    ubicacion = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low));
                _ubicacionUsuario = ubicacion;
            }
            catch { }
        }

        private string CalcularDistancia(double lat, double lng)
        {
            if (_ubicacionUsuario == null || lat == 0 || lng == 0) return "";
            var d = Location.CalculateDistance(_ubicacionUsuario.Latitude, _ubicacionUsuario.Longitude, lat, lng, DistanceUnits.Kilometers);
            return d < 1 ? $"{(int)(d * 1000)} m" : $"{d:F1} km";
        }

        private async Task CargarDesdeApi()
        {
            // Banners
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var banners = await http.GetFromJsonAsync<List<BannerDto>>($"{BASE_URL}/api/Banners/seccion/tiendas");
                BannerTiendasCarousel.ItemsSource = banners != null && banners.Count > 0
                    ? banners.Select(b => $"{BASE_URL}{b.ImagenUrl}").ToList()
                    : new List<string> { "banner1.png", "banner2.png" };
            }
            catch { BannerTiendasCarousel.ItemsSource = new List<string> { "banner1.png", "banner2.png" }; }

            // Tiendas
            try
            {
                var tiendas = await _comercioService.ObtenerPorRubroAsync("tiendas");
                if (tiendas != null)
                {
                    foreach (var t in tiendas)
                    {
                        if (!string.IsNullOrWhiteSpace(t.logoUrl) && !t.logoUrl.StartsWith("http"))
                            t.logoUrl = $"{BASE_URL}{t.logoUrl}";
                        t.distancia = CalcularDistancia(t.latitud, t.longitud);
                    }
                    _todasTiendas = tiendas;
                }
            }
            catch { }

            // Categorías desde API
            List<CategoriaItem> categorias = new();
            try
            {
                using var httpCat = new System.Net.Http.HttpClient();
                var categoriasApi = await httpCat.GetFromJsonAsync<List<CategoriaApiDto>>($"{BASE_URL}/api/Categorias/seccion/tiendas");
                categorias = categoriasApi?
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Orden)
                    .Select(c => new CategoriaItem
                    {
                        Nombre = c.Nombre,
                        Icono = string.IsNullOrWhiteSpace(c.IconoUrl) ? "icono_default.png"
                            : c.IconoUrl.StartsWith("http") ? c.IconoUrl
                            : $"{BASE_URL}{c.IconoUrl}"
                    })
                    .ToList() ?? new List<CategoriaItem>();
            }
            catch { }

            // Destacados desde panel
            List<Local> destacados = new();
            try
            {
                using var httpDest = new System.Net.Http.HttpClient();
                var bannersDest = await httpDest.GetFromJsonAsync<List<BannerDto>>(
                    $"{BASE_URL}/api/Banners/seccion/tiendas-destacados");

                if (bannersDest != null && bannersDest.Count > 0)
                {
                    var destacadosVM = bannersDest.Select(b => {
                        var local = _todasTiendas.FirstOrDefault(l => l.id == b.LocalId);
                        return new DestacadoTiendaVM
                        {
                            Local = local,
                            nombre = local?.nombre ?? "",
                            distancia = local != null ? CalcularDistancia(local.latitud, local.longitud) : "",
                            ImagenBanner = $"{BASE_URL}{b.ImagenUrl}"
                        };
                    }).Where(x => x.Local != null).ToList();

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ListaCategorias.ItemsSource = categorias;
                        ListaTiendas.ItemsSource = destacadosVM;
                    });
                    return;
                }
            }
            catch { }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ListaCategorias.ItemsSource = categorias;
                ListaTiendas.ItemsSource = destacados.Count > 0
                    ? destacados
                    : new List<Local>();
            });
        }

        private async void OnCategoriaTapped(object sender, EventArgs e)
        {
            if (e is not TappedEventArgs tapped || tapped.Parameter is not string nombre) return;
            await Shell.Current.GoToAsync($"tiendas-lista?categoria={Uri.EscapeDataString(nombre)}");
        }

        private async void OnTiendaTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not Local local) return;
            await Shell.Current.GoToAsync($"tienda-local?id={local.id}");
        }

        private async void AbrirBuscador_Tapped(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("buscador");

        private void IniciarCarrusel()
        {
            bannerTimer = new System.Timers.Timer(2500);
            bannerTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (BannerTiendasCarousel.ItemsSource is List<string> items && items.Count > 0)
                        BannerTiendasCarousel.Position = (BannerTiendasCarousel.Position + 1) % items.Count;
                });
            };
            bannerTimer.Start();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            bannerTimer?.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            bannerTimer?.Stop();
        }
    }

    public class DestacadoTiendaVM
    {
        public Local? Local { get; set; }
        public string nombre { get; set; } = "";
        public string distancia { get; set; } = "";
        public string ImagenBanner { get; set; } = "";
    }
}