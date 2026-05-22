using ClickYa.Models;
using ClickYa.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;

namespace ClickYa.Views
{
    public partial class LocalesPage : ContentPage
    {
        private System.Timers.Timer bannerTimer;
        private readonly ComerciosService _comercioService = new();
        private const string BASE_URL = "http://192.168.100.9:5191";
        private Location? _ubicacionUsuario;
        private List<CategoriaItem> _categorias = new();
        private List<Local> _todosLocales = new();

        public ObservableCollection<Local> LocalesFiltrados { get; set; } = new();

        public LocalesPage()
        {
            InitializeComponent();
            BindingContext = this;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsEnabled = true });
            _ = IniciarConUbicacion();
            IniciarCarrusel();
        }

        private async Task IniciarConUbicacion()
        {
            await ObtenerUbicacionUsuario();
            await CargarLocalesDesdeApi();
        }

        private async Task ObtenerUbicacionUsuario()
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

        private string CalcularDistancia(double latLocal, double lngLocal)
        {
            if (_ubicacionUsuario == null || latLocal == 0 || lngLocal == 0)
                return "";
            var d = Location.CalculateDistance(
                _ubicacionUsuario.Latitude, _ubicacionUsuario.Longitude,
                latLocal, lngLocal, DistanceUnits.Kilometers);
            return d < 1 ? $"{(int)(d * 1000)} m" : $"{d:F1} km";
        }

        private async Task CargarLocalesDesdeApi()
        {
            try
            {
                // Banners
                try
                {
                    using var http = new System.Net.Http.HttpClient();
                    var banners = await http.GetFromJsonAsync<List<BannerDto>>(
                        $"{BASE_URL}/api/Banners/seccion/comidas");
                    BannerComidas.ItemsSource = banners != null && banners.Count > 0
                        ? banners.Select(b => $"{BASE_URL}{b.ImagenUrl}").ToList()
                        : new List<string> { "banner1.png", "banner2.png" };
                }
                catch
                {
                    BannerComidas.ItemsSource = new List<string> { "banner1.png", "banner2.png" };
                }

                // Promociones
                try
                {
                    using var httpPromo = new System.Net.Http.HttpClient();
                    var promos = await httpPromo.GetFromJsonAsync<List<BannerDto>>(
                         $"{BASE_URL}/api/Banners/seccion/comidas-promos");

                    var promoItems = promos != null && promos.Count > 0
                        ? promos.Select(b => new BannerPromoItem
                        {
                            LocalId = b.LocalId,
                            ImagenCompleta = $"{BASE_URL}{b.ImagenUrl}"
                        }).ToList()
                        : new List<BannerPromoItem>();

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ListaPromociones.ItemsSource = promoItems;
                    });
                }
                catch { }

                // Locales
                List<Local> locales = new();
                try { var l1 = await _comercioService.ObtenerPorRubroAsync("comidas"); if (l1 != null) locales.AddRange(l1); } catch { }
                try { var l2 = await _comercioService.ObtenerPorRubroAsync("comida"); if (l2 != null) locales.AddRange(l2); } catch { }

                foreach (var l in locales)
                {
                    if (!string.IsNullOrWhiteSpace(l.logoUrl) && !l.logoUrl.StartsWith("http"))
                        l.logoUrl = $"{BASE_URL}{l.logoUrl}";
                    l.distancia = CalcularDistancia(l.latitud, l.longitud);
                }

                _todosLocales = locales;

                // Categorías desde API
                try
                {
                    using var httpCat = new System.Net.Http.HttpClient();
                    var categoriasApi = await httpCat.GetFromJsonAsync<List<CategoriaApiDto>>(
                        $"{BASE_URL}/api/Categorias/seccion/comidas");

                    _categorias = categoriasApi?
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
                catch { _categorias = new List<CategoriaItem>(); }

                // Destacados
                List<Local> destacados = new();
                try
                {
                    using var httpDest = new System.Net.Http.HttpClient();
                    var bannersDest = await httpDest.GetFromJsonAsync<List<BannerDto>>(
                        $"{BASE_URL}/api/Banners/seccion/comidas-destacados");

                    if (bannersDest != null && bannersDest.Count > 0)
                    {
                        var destacadosVM = bannersDest.Select(b => {
                            var local = _todosLocales.FirstOrDefault(l => l.id == b.LocalId);
                            return new DestacadoComidaVM
                            {
                                Local = local,
                                nombre = local?.nombre ?? "",
                                distancia = local != null ? CalcularDistancia(local.latitud, local.longitud) : "",
                                ImagenBanner = $"{BASE_URL}{b.ImagenUrl}"
                            };
                        }).Where(x => x.Local != null).ToList();

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ListaCategorias.ItemsSource = _categorias;
                            ListaLocales.ItemsSource = destacadosVM;
                        });
                        return;
                    }
                }
                catch { }


                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ListaCategorias.ItemsSource = _categorias;
                    LocalesFiltrados.Clear();
                    foreach (var l in destacados)
                        LocalesFiltrados.Add(l);
                    ListaLocales.ItemsSource = LocalesFiltrados;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR LocalesPage: " + ex.Message);
            }
        }

        private void OnCategoriaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not CategoriaItem cat) return;
            ((CollectionView)sender).SelectedItem = null;
            FiltrarPorCategoria(cat.Nombre);
        }

        private async void OnCategoriaTapped(object sender, EventArgs e)
        {
            if (e is not TappedEventArgs tapped || tapped.Parameter is not string nombre) return;
            await Shell.Current.GoToAsync($"comida-lista?categoria={Uri.EscapeDataString(nombre)}");
        }

        private async void FiltrarPorCategoria(string nombre)
        {
            var keyNorm = Normalizar(nombre);
            var filtrados = _todosLocales.Where(l =>
            {
                var catNorm = Normalizar(l.categoria ?? "");
                return catNorm == keyNorm;
            }).ToList();
            LocalesFiltrados.Clear();
            foreach (var l in filtrados)
                LocalesFiltrados.Add(l);
        }

        private string Normalizar(string texto)
        {
            return texto.ToLower()
                .Replace("í", "i").Replace("é", "e").Replace("á", "a")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
        }
        private async void AbrirBuscador_Tapped(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("buscador");
        private async void OnLocalTapped(object sender, EventArgs e)
        {
            Local? local = null;
            if (e is TappedEventArgs tapped && tapped.Parameter is Local l)
                local = l;
            else if (sender is BindableObject bo && bo.BindingContext is Local l2)
                local = l2;
            if (local == null || local.id == -1) return;
            await Shell.Current.GoToAsync($"local?id={local.id}");
        }

        private async void OnPromoTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not BannerPromoItem banner) return;
            if (banner.LocalId <= 0) return;
            await Shell.Current.GoToAsync($"local?id={banner.LocalId}");
        }

        private void IniciarCarrusel()
        {
            bannerTimer = new System.Timers.Timer(2500);
            bannerTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (BannerComidas.ItemsSource is List<string> items && items.Count > 0)
                        BannerComidas.Position = (BannerComidas.Position + 1) % items.Count;
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

    public class CategoriaItem
    {
        public string Nombre { get; set; } = "";
        public string Icono { get; set; } = "";
    }

    public class CategoriaApiDto
    {
        public int Id { get; set; }
        public string Seccion { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string IconoUrl { get; set; } = "";
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class BannerPromoItem
    {
        public int LocalId { get; set; }
        public string ImagenCompleta { get; set; } = "";
    }
    public class DestacadoComidaVM
    {
        public Local? Local { get; set; }
        public string nombre { get; set; } = "";
        public string distancia { get; set; } = "";
        public string ImagenBanner { get; set; } = "";
    }
}