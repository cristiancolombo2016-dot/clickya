using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using ClickYa.Services;

namespace ClickYa.Views;

[QueryProperty(nameof(LocalId), "id")]
[QueryProperty(nameof(NombreBar), "nombre")]
public partial class BarPage : ContentPage
{
    private readonly ClickYaDataService _dataService = new();
    private const string BASE_URL = "https://clickya-production.up.railway.app";

    private int _idLocal = 0;
    private string _whatsApp = "";
    private string _instagram = "";
    private string _ubicacion = "";
    private string _nombre = "Bar";

    public ObservableCollection<PublicacionBar> Publicaciones { get; set; }
    public ObservableCollection<string> Galeria { get; set; }

    public string NombreBar { set => _nombre = value; }

    public string LocalId
    {
        set
        {
            if (int.TryParse(value, out int id))
                _ = CargarDesdeApi(id);
        }
    }

    public BarPage()
    {
        InitializeComponent();

        Publicaciones = new ObservableCollection<PublicacionBar>
        {
            new PublicacionBar { Titulo = "Happy Hour", Descripcion = "Todos los días 18:00 - 20:00", Precio = "$2500", Imagen = "bar2.png" },
            new PublicacionBar { Titulo = "Promo Burgers", Descripcion = "2x1 en burgers", Precio = "$4500", Imagen = "bar3.png" },
            new PublicacionBar { Titulo = "Música en vivo", Descripcion = "Este sábado 22:30 hs", Precio = "Entrada libre", Imagen = "bar4.png" },
            new PublicacionBar { Titulo = "Promo Cervezas", Descripcion = "2x1 en pintas", Precio = "$3000", Imagen = "bar5.png" }
        };

        Galeria = new ObservableCollection<string>
        {
            "bar6.png", "bar7.png", "bar8.png", "bar9.png"
        };

        BindingContext = this;
    }

    private async Task CargarDesdeApi(int id)
    {
        try
        {
            var local = await _dataService.ObtenerLocalAsync(id);
            if (local == null) return;

            _idLocal = id;
            _whatsApp = local.whatsApp ?? "";
            _instagram = local.instagram ?? "";
            _ubicacion = local.ubicacion ?? "";
            _nombre = local.nombre ?? "";

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!string.IsNullOrWhiteSpace(local.portadaUrl))
                    ImgPortada.Source = $"{BASE_URL}{local.portadaUrl}";

                if (!string.IsNullOrWhiteSpace(local.logoUrl))
                    ImgLogo.Source = $"{BASE_URL}{local.logoUrl}";

                LblNombre.Text = local.nombre ?? "";
                LblDireccion.Text = local.ubicacion ?? "";
            });

            var publicaciones = await _dataService.ObtenerPublicacionesAsync(id);
            if (publicaciones != null && publicaciones.Count > 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Publicaciones.Clear();
                    Galeria.Clear();

                    foreach (var p in publicaciones)
                    {
                        var imgUrl = string.IsNullOrWhiteSpace(p.ImagenUrl)
                            ? "bar1.png"
                            : $"{BASE_URL}{p.ImagenUrl}";

                        Publicaciones.Add(new PublicacionBar
                        {
                            Imagen = imgUrl,
                            Titulo = p.Titulo ?? "",
                            Precio = p.Precio ?? "",
                            Descripcion = p.Descripcion ?? ""
                        });

                        if (p.ImagenesUrls != null && p.ImagenesUrls.Count > 0)
                        {
                            foreach (var url in p.ImagenesUrls.Take(15))
                                Galeria.Add(url.StartsWith("http") ? url : $"{BASE_URL}{url}");
                        }
                        else
                        {
                            Galeria.Add(imgUrl);
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error BarPage: " + ex.Message);
        }
    }

    private async void OnVolverTapped(object sender, TappedEventArgs e)
        => await Navigation.PopAsync();

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_whatsApp)) return;

        // Limpia el número y arma el formato WhatsApp Argentina: 549 + caracteristica + numero
        var soloNumeros = new string(_whatsApp.Where(char.IsDigit).ToArray());

        if (soloNumeros.StartsWith("0"))
            soloNumeros = soloNumeros.Substring(1);

        string numeroFinal;
        if (soloNumeros.StartsWith("549"))
            numeroFinal = soloNumeros;
        else if (soloNumeros.StartsWith("54"))
            numeroFinal = "549" + soloNumeros.Substring(2);
        else
            numeroFinal = "549" + soloNumeros;

        await Launcher.Default.OpenAsync($"https://wa.me/{numeroFinal}");
    }

    private async void OnInstagramClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_instagram)) return;

        var url = _instagram.StartsWith("http")
            ? _instagram
            : $"https://instagram.com/{_instagram.Replace("@", "")}";

        await Launcher.Default.OpenAsync(url);
    }

    private async void OnUbicacionClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_ubicacion))
            await Launcher.Default.OpenAsync(
                $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(_ubicacion)}");
    }

    private async void OnCompartirClicked(object sender, EventArgs e)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = _nombre,
            Text = $"{_nombre}\nhttps://clickya.com"
        });
    }

    private async void OnPublicacionTapped(object sender, EventArgs e)
    {
        if (sender is not Image img) return;

        var btnCerrar = new Button
        {
            Text = "✕",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            FontSize = 24,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 40, 16, 0)
        };

        var imgVisor = new Image
        {
            Source = img.Source,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill
        };

        var grid = new Grid { BackgroundColor = Colors.Black };
        grid.Children.Add(imgVisor);
        grid.Children.Add(btnCerrar);

        var modalPage = new ContentPage { BackgroundColor = Colors.Black };
        btnCerrar.Clicked += async (s, ev) => await Navigation.PopModalAsync();
        modalPage.Content = grid;

        await Navigation.PushModalAsync(modalPage);
    }

    private async void OnGaleriaTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string url) return;
        var imagenes = Galeria.ToList();
        await Shell.Current.GoToAsync(
            $"galeria-pub?titulo={Uri.EscapeDataString(_nombre)}&imgs={Uri.EscapeDataString(string.Join(",", imagenes))}");
    }

    // ============================================
    // REPORTAR COMERCIO
    // ============================================
    private async void OnReportarTapped(object sender, EventArgs e)
    {
        if (_idLocal == 0) return;

        // Menú de opciones (estilo Instagram)
        string accion = await DisplayActionSheet(
            "Opciones", "Cancelar", null, "🚩 Reportar comercio");

        if (accion != "🚩 Reportar comercio") return;

        // Elegir el motivo del reporte
        string motivo = await DisplayActionSheet(
            "¿Por qué querés reportar este comercio?", "Cancelar", null,
            "Contenido inapropiado",
            "Información falsa o engañosa",
            "Estafa o fraude",
            "No existe / cerró",
            "Otro");

        if (string.IsNullOrWhiteSpace(motivo) || motivo == "Cancelar") return;

        // Enviar el reporte a la API
        try
        {
            var reporte = new
            {
                comercioId = _idLocal,
                nombreComercio = _nombre,
                motivo = motivo,
                detalle = ""
            };

            var json = System.Text.Json.JsonSerializer.Serialize(reporte);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var http = new HttpClient();
            var resp = await http.PostAsync($"{BASE_URL}/api/Reportes", content);

            if (resp.IsSuccessStatusCode)
                await DisplayAlert("Gracias", "Tu reporte fue enviado. Lo vamos a revisar.", "OK");
            else
                await DisplayAlert("Error", "No se pudo enviar el reporte. Intentá de nuevo.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("ERROR reporte: " + ex.Message);
            await DisplayAlert("Error", "No se pudo enviar el reporte. Revisá tu conexión.", "OK");
        }
    }
}

public class PublicacionBar
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Precio { get; set; } = string.Empty;
    public string Imagen { get; set; } = string.Empty;
}