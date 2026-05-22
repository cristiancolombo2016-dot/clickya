using ClickYa.Models;
using ClickYa.Services;
using System.Net.Http.Json;

namespace ClickYa.Views
{
    public class ResultadoBusqueda
    {
        public string Nombre { get; set; } = "";
        public string Subtitulo { get; set; } = "";
        public string Logo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string ColorTipo { get; set; } = "#4B0082";
        public string DistanciaTexto { get; set; } = "";
        public bool TieneDistancia => !string.IsNullOrWhiteSpace(DistanciaTexto);
        public string TipoNavegacion { get; set; } = "";
        public int Id { get; set; }
        public double Km { get; set; } = double.MaxValue;
    }

    public partial class BuscadorPage : ContentPage
    {
        private const string BASE_URL = "http://192.168.100.9:5191";
        private readonly ComerciosService _comercioService = new();
        private Location? _ubicacionUsuario;
        private List<ResultadoBusqueda> _todosResultados = new();
        private string _filtroActivo = "todo";
        private CancellationTokenSource? _debounce;

        public BuscadorPage()
        {
            InitializeComponent();
            _ = Inicializar();
        }

        private async Task Inicializar()
        {
            EstadoCargando.IsVisible = true;
            EstadoInicial.IsVisible = false;
            await ObtenerUbicacion();
            await CargarDatos();
            EstadoCargando.IsVisible = false;
            EstadoInicial.IsVisible = true;
            EntryBuscar.Focus();
        }

        private async Task ObtenerUbicacion()
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

        private double CalcularKm(double lat, double lng)
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

        private async Task CargarDatos()
        {
            _todosResultados = new List<ResultadoBusqueda>();

            try
            {
                var comercios = await _comercioService.ObtenerTodosAsync();
                if (comercios != null)
                {
                    foreach (var c in comercios)
                    {
                        var rubro = (c.rubro ?? "").ToLower();
                        var (tipo, color) = rubro switch
                        {
                            "comidas" or "comida" => ("Comida", "#E65100"),
                            "tiendas" or "tienda" => ("Tienda", "#0F6E56"),
                            "bares" or "bar" => ("Bar", "#185FA5"),
                            _ => ("Local", "#4B0082")
                        };

                        var km = CalcularKm(c.latitud, c.longitud);

                        _todosResultados.Add(new ResultadoBusqueda
                        {
                            Id = c.id,
                            Nombre = c.nombre ?? "",
                            Subtitulo = c.categoria ?? "",
                            Logo = string.IsNullOrWhiteSpace(c.logoUrl) ? "logo_clickya.png"
                                : c.logoUrl.StartsWith("http") ? c.logoUrl
                                : $"{BASE_URL}{c.logoUrl}",
                            Tipo = tipo,
                            ColorTipo = color,
                            Km = km,
                            DistanciaTexto = FormatearDistancia(km),
                            TipoNavegacion = "local"
                        });
                    }
                }
            }
            catch { }

            try
            {
                using var http = new System.Net.Http.HttpClient();
                var pubs = await http.GetFromJsonAsync<List<PublicacionComercioDto>>(
                    $"{BASE_URL}/api/Publicaciones/todas");

                if (pubs != null)
                {
                    foreach (var p in pubs)
                    {
                        var comercio = _todosResultados
                            .FirstOrDefault(r => r.TipoNavegacion == "local" && r.Id == p.ComercioId);

                        _todosResultados.Add(new ResultadoBusqueda
                        {
                            Id = p.ComercioId,
                            Nombre = p.Titulo ?? "",
                            Subtitulo = $"Plato en {comercio?.Nombre ?? "un local"}",
                            Logo = string.IsNullOrWhiteSpace(p.ImagenUrl) ? "logo_clickya.png"
                                : p.ImagenUrl.StartsWith("http") ? p.ImagenUrl
                                : $"{BASE_URL}{p.ImagenUrl}",
                            Tipo = "Plato",
                            ColorTipo = "#B8008A",
                            Km = comercio?.Km ?? double.MaxValue,
                            DistanciaTexto = comercio?.DistanciaTexto ?? "",
                            TipoNavegacion = "local"
                        });
                    }
                }
            }
            catch { }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _debounce?.Cancel();
            _debounce = new CancellationTokenSource();
            var token = _debounce.Token;

            Task.Delay(200, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    MainThread.BeginInvokeOnMainThread(() => Buscar(e.NewTextValue ?? ""));
            });
        }

        private void Buscar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                ListaResultados.IsVisible = false;
                EstadoVacio.IsVisible = false;
                EstadoInicial.IsVisible = true;
                return;
            }

            var norm = Normalizar(texto);

            var filtrados = _todosResultados
                .Where(r =>
                {
                    if (_filtroActivo == "comidas" && r.Tipo != "Comida" && r.Tipo != "Plato") return false;
                    if (_filtroActivo == "tiendas" && r.Tipo != "Tienda") return false;

                    if (r.Tipo == "Plato")
                        return Normalizar(r.Nombre).Contains(norm);

                    return Normalizar(r.Nombre).Contains(norm) ||
                           Normalizar(r.Subtitulo).Contains(norm);
                })
                .OrderBy(r => r.Km)
                .ToList();

            EstadoInicial.IsVisible = false;

            if (filtrados.Count == 0)
            {
                ListaResultados.IsVisible = false;
                EstadoVacio.IsVisible = true;
            }
            else
            {
                EstadoVacio.IsVisible = false;
                ListaResultados.IsVisible = true;
                ListaResultados.ItemsSource = filtrados;
            }
        }

        private string Normalizar(string texto)
        {
            return texto.ToLower()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
        }

        private void AplicarFiltro(string filtro)
        {
            _filtroActivo = filtro;

            var chips = new[] { ChipTodo, ChipComidas, ChipTiendas };
            var filtros = new[] { "todo", "comidas", "tiendas" };

            for (int i = 0; i < chips.Length; i++)
            {
                bool activo = filtros[i] == filtro;
                chips[i].BackgroundColor = activo
                    ? Color.FromArgb("#4B0082")
                    : Colors.Transparent;
                if (chips[i].Content is Label lbl)
                    lbl.TextColor = activo ? Colors.White : Color.FromArgb("#CCC");
            }

            Buscar(EntryBuscar.Text ?? "");
        }

        private void FiltroTodo_Tapped(object sender, EventArgs e) => AplicarFiltro("todo");
        private void FiltroComidas_Tapped(object sender, EventArgs e) => AplicarFiltro("comidas");
        private void FiltroTiendas_Tapped(object sender, EventArgs e) => AplicarFiltro("tiendas");

        private async void OnResultadoTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not ResultadoBusqueda resultado) return;

            if (resultado.TipoNavegacion == "local")
                await Shell.Current.GoToAsync($"local?id={resultado.Id}");
        }

        private async void Volver_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public class PublicacionComercioDto
    {
        public int Id { get; set; }
        public int ComercioId { get; set; }
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public string Precio { get; set; } = "";
    }
}