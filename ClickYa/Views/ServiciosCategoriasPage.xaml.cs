using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;

namespace ClickYa.Views;

public partial class ServiciosCategoriasPage : ContentPage
{
    private System.Timers.Timer? _bannerTimer;
    private const string BASE_URL = "http://192.168.100.9:5191";
    private List<BannerDto> _bannersServicio = new();

    public ObservableCollection<CategoriaServicio> Categorias { get; set; }
    public ObservableCollection<ServicioDestacado> ServiciosDestacados { get; set; }

    public ServiciosCategoriasPage()
    {
        InitializeComponent();

        Categorias = new ObservableCollection<CategoriaServicio>
        {
            new CategoriaServicio { Nombre = "Electricista", Imagen = "electricos1.jfif" },
            new CategoriaServicio { Nombre = "Refrigeración", Imagen = "refrigeracion2.jfif" },
            new CategoriaServicio { Nombre = "Plomería", Imagen = "gasista1.png" },
            new CategoriaServicio { Nombre = "Gasista", Imagen = "gasista1.jfif" },
            new CategoriaServicio { Nombre = "Carpintería", Imagen = "carpintero2.jfif" }
        };

        ServiciosDestacados = new ObservableCollection<ServicioDestacado>();

        BindingContext = this;
        CargarBanners();
        CargarDestacados();
    }

    private async void CargarCategorias()
    {
        try
        {
            using var http = new HttpClient();
            var categorias = await http.GetFromJsonAsync<List<CategoriaApiDto>>(
                $"{BASE_URL}/api/Categorias/seccion/servicios");

            if (categorias != null && categorias.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Categorias.Clear();
                    foreach (var c in categorias.Where(c => c.Activo).OrderBy(c => c.Orden))
                    {
                        Categorias.Add(new CategoriaServicio
                        {
                            Nombre = c.Nombre,
                            Imagen = string.IsNullOrWhiteSpace(c.IconoUrl) ? "electricos1.jfif"
                                : c.IconoUrl.StartsWith("http") ? c.IconoUrl
                                : $"{BASE_URL}{c.IconoUrl}"
                        });
                    }
                });
            }
        }
        catch { }
    }
    private async void CargarBanners()
    {
        try
        {
            using var http = new HttpClient();
            var banners = await http.GetFromJsonAsync<List<BannerDto>>(
                $"{BASE_URL}/api/Banners/seccion/servicios");

            if (banners != null && banners.Count > 0)
            {
                _bannersServicio = banners;
                var urls = banners.Select(b => $"{BASE_URL}{b.ImagenUrl}").ToList();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    carouselBanners.ItemsSource = urls;
                    IniciarCarrusel();
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    carouselBanners.ItemsSource = new List<string>
                    {
                        "electricos1.jfif",
                        "refrigeracion1.jpg",
                        "gasista1.png",
                        "carpintero2.jfif"
                    };
                    IniciarCarrusel();
                });
            }
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                carouselBanners.ItemsSource = new List<string>
                {
                    "electricos1.jfif",
                    "refrigeracion1.jpg",
                    "gasista1.png",
                    "carpintero2.jfif"
                };
                IniciarCarrusel();
            });
        }
    }

    private async void CargarDestacados()
    {
        try
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync($"{BASE_URL}/api/Tecnico");
            var opciones = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tecnicos = System.Text.Json.JsonSerializer.Deserialize<List<TecnicoItem>>(json, opciones) ?? new();

            var premiums = tecnicos.Where(t => t.EsPremium && t.Activo).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ServiciosDestacados.Clear();
                foreach (var t in premiums)
                {
                    ServiciosDestacados.Add(new ServicioDestacado
                    {
                        Nombre = t.Nombre,
                        Descripcion = t.Rubro,
                        Imagen = string.IsNullOrEmpty(t.Logo)
        ? "electricos1.jfif"
        : $"{BASE_URL}{t.Logo}",
                        TecnicoId = t.Id
                    });
                }
            });
        }
        catch { }
    }

    private void IniciarCarrusel()
    {
        _bannerTimer?.Stop();
        _bannerTimer?.Dispose();

        _bannerTimer = new System.Timers.Timer(2500);
        _bannerTimer.AutoReset = true;
        _bannerTimer.Elapsed += (s, e) =>
        {
            Dispatcher.Dispatch(() =>
            {
                var items = carouselBanners.ItemsSource as IEnumerable<string>;
                if (items == null) return;
                var list = items as List<string> ?? items.ToList();
                if (list.Count < 2) return;
                carouselBanners.Position = (carouselBanners.Position + 1) % list.Count;
            });
        };
        _bannerTimer.Start();
    }

    private async void Banner_Tapped(object sender, EventArgs e)
    {
        var pos = carouselBanners.Position;
        if (_bannersServicio.Count == 0 || pos >= _bannersServicio.Count) return;
        var banner = _bannersServicio[pos];
        if (banner.TecnicoId > 0)
            await Shell.Current.GoToAsync($"perfil-tecnico?id={banner.TecnicoId}");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _bannerTimer?.Stop();
        _buscarTimer?.Stop();
    }

    protected override void OnAppearing()
    {
        CargarBanners();
        CargarCategorias();
        CargarDestacados();
    }

    private async void BtnUrgencia_Clicked(object sender, EventArgs e)
    {
        var rubros = new string[]
        {
            "Electricista", "Gasista", "Plomero",
            "Aire acondicionado", "Carpintería", "Cerrajería", "Otro"
        };

        var seleccion = await DisplayActionSheet(
            "¿Qué tipo de servicio necesitás?",
            "Cancelar", null, rubros);

        if (seleccion == null || seleccion == "Cancelar") return;

        await Shell.Current.GoToAsync(
            $"solicitar-servicio?rubro={Uri.EscapeDataString(seleccion)}");
    }
    private async void AbrirBuscador_Tapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("buscador");
    private async void OnCategoriaTapped(object sender, TappedEventArgs e)
    {
        var rubro = e.Parameter?.ToString();
        if (string.IsNullOrEmpty(rubro)) return;
        await Shell.Current.GoToAsync(
            $"tecnicos-rubro?rubro={Uri.EscapeDataString(rubro)}");
    }
    private async void OnDestacadoTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ServicioDestacado sd) return;
        if (sd.TecnicoId <= 0) return;
        await Shell.Current.GoToAsync($"perfil-tecnico?id={sd.TecnicoId}");
    }

    private List<string> _todosLosRubros = new()
    {
        "Electricista", "Gasista", "Plomero",
        "Aire acondicionado", "Carpintería", "Cerrajería",
        "Refrigeración", "Plomería", "Pintor", "Techista"
    };

    private System.Timers.Timer? _buscarTimer;

    private void OnBuscarChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.Trim() ?? "";
        if (string.IsNullOrEmpty(texto) || texto.Length < 2) return;

        _buscarTimer?.Stop();
        _buscarTimer?.Dispose();

        _buscarTimer = new System.Timers.Timer(1000);
        _buscarTimer.AutoReset = false;
        _buscarTimer.Elapsed += async (s, ev) =>
        {
            var coincidencia = _todosLosRubros
                .FirstOrDefault(r => r.ToLower().StartsWith(texto.ToLower()));

            if (coincidencia != null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync(
                        $"tecnicos-rubro?rubro={Uri.EscapeDataString(coincidencia)}");
                });
            }
        };
        _buscarTimer?.Start();
    }
}

public class BannerServicio
{
    public string Imagen { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
}

public class CategoriaServicio
{
    public string Nombre { get; set; } = string.Empty;
    public string Imagen { get; set; } = string.Empty;
}
public class ServicioDestacado
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Imagen { get; set; } = string.Empty;
    public int TecnicoId { get; set; }
}