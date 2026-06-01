using ClickYa.Services;
using ClickYa.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace ClickYa.Views;

[QueryProperty(nameof(LocalId), "id")]
public partial class LocalPage : ContentPage
{
    private Local? _local;
    private readonly ClickYaDataService _dataService = new();

    public string LocalId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _ = CargarLocalDesdeApi(id);
            }
        }
    }

    public Local Local
    {
        get => _local!;
        set
        {
            _local = value;
            CargarLocal();
        }
    }

    public LocalPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);   // ← CAMBIO: era SetBackButtonBehavior
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActualizarBadgeCarrito();
    }

    // ============================================
    // CARGA LOCAL DESDE API
    // ============================================
    private async Task CargarLocalDesdeApi(int id)
    {
        Debug.WriteLine("ENTRANDO A CargarLocalDesdeApi");

        var local = await _dataService.ObtenerLocalAsync(id);

        Debug.WriteLine("LOCAL RECIBIDO ES NULL? " + (local == null));

        if (local == null)
            return;

        _local = local;
        CargarLocal();
    }

    // ============================================
    // CARGA DATOS EN LA VISTA
    // ============================================
    private void CargarLocal()
    {
        if (_local == null)
            return;

        CarritoService.Instancia.SetLocal(_local.nombre);

        var baseUrl = _dataService.BaseUrl;

        ImgPortada.Source = !string.IsNullOrWhiteSpace(_local.portadaUrl)
            ? $"{baseUrl}{_local.portadaUrl}"
            : "pizzeria.png";

        ImgLogo.Source = !string.IsNullOrWhiteSpace(_local.logoUrl)
            ? $"{baseUrl}{_local.logoUrl}"
            : "logo_pizzeria.png";

        LblNombre.Text = _local.nombre ?? "";
        LblDireccion.Text = _local.ubicacion ?? "";
        LblDireccion.IsVisible = !string.IsNullOrWhiteSpace(_local.ubicacion);

        LblDescripcion.Text = !string.IsNullOrWhiteSpace(_local.descripcion)
            ? _local.descripcion
            : "Conocé este local, descubrí sus productos y contactate de forma rápida desde ClickYa.";

        LblHorario.Text = !string.IsNullOrWhiteSpace(_local.horarios)
            ? _local.horarios
            : "Horarios no informados";

        // ── NUEVO: chips de categoría ──
        AgregarChips(_local.categoria ?? "");

        // Heladería
        bool esHeladeria =
            _local.categoria != null &&
            _local.categoria.ToLower().Contains("helad");

        LayoutHelados.IsVisible = esHeladeria;
        LayoutProductos.IsVisible = true;

        _ = CargarPublicacionesAsync();

        ActualizarBadgeCarrito();
    }

    // ============================================
    // CHIPS DE CATEGORÍA  ← NUEVO
    // ============================================
    private void AgregarChips(string categoriaRaw)
    {
        FlexChips.Children.Clear();

        var chips = categoriaRaw
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c));

        foreach (var chip in chips)
        {
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#55FFFFFF"),
                StrokeThickness = 0,
                Padding = new Thickness(10, 4),
                Margin = new Thickness(0, 3, 6, 0),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) }
            };

            border.Content = new Label
            {
                Text = chip,
                FontSize = 12,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };

            FlexChips.Children.Add(border);
        }
    }

    // ============================================
    // CARGAR PUBLICACIONES
    // ============================================
    private async Task CargarPublicacionesAsync()
    {
        Debug.WriteLine("ENTRANDO A CargarPublicacionesAsync");

        try
        {
            if (_local == null) return;

            var publicaciones = await _dataService.ObtenerPublicacionesAsync(_local.id);

            if (publicaciones == null || publicaciones.Count == 0)
                return;

            var lista = publicaciones
                .Select(p =>
                {
                    var urlImagen = $"{_dataService.BaseUrl}{p.ImagenUrl}";
                    Debug.WriteLine("URL IMAGEN: " + urlImagen);

                    return new Articulo
                    {
                        nombre = p.Titulo ?? "",
                        precio = ParsePrecio(p.Precio),
                        imagen = urlImagen,

                        imagenes = p.ImagenesUrls != null && p.ImagenesUrls.Count > 0
                            ? p.ImagenesUrls.Select(url =>
                                url.StartsWith("http") ? url : $"{_dataService.BaseUrl}{url}")
                              .ToList()
                            : new List<string> { urlImagen },

                        datosExtraJson = p.DatosExtraJson ?? "",
                        seccion = ExtraerSeccion(p.DatosExtraJson),
                        cantidad = 1
                    };
                })
                .ToList();

            if (lista.Count > 0)
            {
                var grupos = lista
                    .GroupBy(x =>
                        string.IsNullOrWhiteSpace(x.seccion)
                            ? "Nuestros productos"
                            : x.seccion)
                    .Select(g => new GrupoProductos(g.Key, g.ToList()))
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ListaProductos.ItemsSource = grupos;
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ERROR CargarPublicacionesAsync: " + ex.Message);
        }
    }

    private int ParsePrecio(string? precio)
    {
        if (string.IsNullOrWhiteSpace(precio)) return 0;
        var soloNumeros = new string(precio.Where(char.IsDigit).ToArray());
        return int.TryParse(soloNumeros, out var v) ? v : 0;
    }

    private string ExtraerSeccion(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return "";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("seccion", out var s))
                return s.GetString() ?? "";
            return "";
        }
        catch { return ""; }
    }

    // ============================================
    // BOTONES DE ACCIÓN
    // ============================================
    private async void BtnWhatsApp_Clicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_local?.whatsApp))
            await Launcher.OpenAsync(_local.whatsApp);
    }

    private async void BtnMaps_Clicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_local?.ubicacion))
            await Launcher.OpenAsync(_local.ubicacion);
    }

    private async void BtnInstagram_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_local?.instagram)) return;

        var url = _local.instagram.StartsWith("http")
            ? _local.instagram
            : $"https://instagram.com/{_local.instagram.Replace("@", "")}";

        await Launcher.OpenAsync(url);
    }

    private async void BtnCompartir_Clicked(object sender, EventArgs e)
    {
        if (_local == null) return;
        await Share.RequestAsync(new ShareTextRequest
        {
            Title = _local.nombre,
            Text = $"Mirá {_local.nombre} en ClickYa!\nWhatsApp: {_local.whatsApp}"
        });
    }

    // ── Botón volver flotante  ← NUEVO ────────────────────────
    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // ============================================
    // CARRITO
    // ============================================
    private async void AgregarCarrito_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Articulo producto) return;

        CarritoService.Instancia.Agregar(producto);
        ActualizarBadgeCarrito();

        await DisplayAlert("Carrito", $"{producto.nombre} agregado al carrito.", "OK");
    }

    private void ActualizarBadgeCarrito()
    {
        int cantidad = CarritoService.Instancia.Items.Sum(i => i.cantidad);

        if (cantidad > 0)
        {
            LblCantidadCarrito.Text = cantidad.ToString();
            BadgeCarrito.IsVisible = true;
        }
        else
        {
            BadgeCarrito.IsVisible = false;
        }
    }

    private async void AbrirCarrito_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("carrito");
    }

    private async void AbrirHeladoModal_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new HeladoModalPage(_local?.id ?? 0, _local?.whatsApp ?? ""));
    }

    private async void OnProductoTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Articulo producto) return;

        await Shell.Current.GoToAsync("detalleproducto", new Dictionary<string, object>
        {
            { "producto", producto }
        });
    }
}

// ============================================
// MODELO DE GRUPO
// ============================================
public class GrupoProductos : List<Articulo>
{
    public string Nombre { get; set; } = "";

    public GrupoProductos(string nombre, IEnumerable<Articulo> productos) : base(productos)
    {
        Nombre = nombre;
    }
}
