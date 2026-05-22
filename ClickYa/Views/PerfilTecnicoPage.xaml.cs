using System.Text.Json;
namespace ClickYa.Views;
[QueryProperty(nameof(TecnicoId), "id")]
public partial class PerfilTecnicoPage : ContentPage
{
    private const string BASE_URL = "http://192.168.100.9:5191";
    private string _whatsApp = "";
    public string TecnicoId
    {
        set => CargarPerfil(value);
    }
    public PerfilTecnicoPage()
    {
        InitializeComponent();
    }
    private async void CargarPerfil(string id)
    {
        System.Diagnostics.Debug.WriteLine("CARGANDO TECNICO ID: " + id);
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonTecnico = await http.GetStringAsync($"{BASE_URL}/api/Tecnico/{id}");
            var tecnico = JsonSerializer.Deserialize<TecnicoPerfilDto>(jsonTecnico, opciones);
            if (tecnico == null) return;
            _whatsApp = tecnico.WhatsApp;
            LblNombre.Text = tecnico.Nombre;
            LblRubro.Text = tecnico.Rubro;
            LblDireccion.Text = string.IsNullOrEmpty(tecnico.Direccion)
                ? "" : "📍 " + tecnico.Direccion;
            LblDescripcion.Text = tecnico.Descripcion;
            if (!string.IsNullOrEmpty(tecnico.Instagram))
            {
                LblInstagram.Text = "📷 " + tecnico.Instagram;
                LblInstagram.IsVisible = true;
                LblInstagram.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        var ig = tecnico.Instagram.Replace("@", "").Trim();
                        await Launcher.OpenAsync($"https://instagram.com/{ig}");
                    })
                });
            }

            if (!string.IsNullOrEmpty(tecnico.Ubicacion))
            {
                LblUbicacion.Text = "📍 Ver en Google Maps";
                LblUbicacion.IsVisible = true;
                LblUbicacion.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        var ubicacion = tecnico.Ubicacion;
                        if (!ubicacion.StartsWith("http"))
                            ubicacion = $"https://maps.google.com/?q={Uri.EscapeDataString(ubicacion)}";
                        await Launcher.OpenAsync(ubicacion);
                    })
                });
            }
           ;
            if (!string.IsNullOrEmpty(tecnico.FotoPortada))
                ImgPortada.Source = ImageSource.FromUri(new Uri(BASE_URL + tecnico.FotoPortada));
            if (!string.IsNullOrEmpty(tecnico.Logo))
            {
                ImgLogo.Source = ImageSource.FromUri(new Uri(BASE_URL + tecnico.Logo));
                ImgLogo.IsVisible = true;
            }
            var jsonPubs = await http.GetStringAsync(
    $"{BASE_URL}/api/Publicacion/tecnico/{id}");
            System.Diagnostics.Debug.WriteLine("PUBS JSON: " + jsonPubs);
            var pubs = JsonSerializer.Deserialize<List<PublicacionItem>>(jsonPubs, opciones) ?? new();
            System.Diagnostics.Debug.WriteLine("PUBS COUNT: " + pubs.Count);
            ListaPublicaciones.ItemsSource = pubs;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error cargando perfil: " + ex.Message);
        }
    }
    private async void OnVolverTapped(object sender, EventArgs e)
        => await Navigation.PopAsync();
    private async void OnWhatsAppTapped(object sender, EventArgs e)
    {
        var url = $"https://wa.me/54{_whatsApp}";
        await Launcher.OpenAsync(url);
    }
    private async void OnCompartirTapped(object sender, EventArgs e)
    {
        await Share.RequestAsync(new ShareTextRequest
        {
            Title = LblNombre.Text,
            Text = $"Mirá el perfil de {LblNombre.Text} ({LblRubro.Text}) en ClickYa!\n" +
                   $"WhatsApp: {_whatsApp}"
        });
    }
    private async void OnPublicacionTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PublicacionItem pub) return;
        if (pub.Imagenes == null || pub.Imagenes.Count == 0) return;

        var imagenes = pub.Imagenes
            .Select(i => "http://192.168.100.9:5191" + i)
            .ToList();

        await Shell.Current.GoToAsync(
            $"galeria-pub?titulo={Uri.EscapeDataString(pub.Titulo)}&imgs={Uri.EscapeDataString(string.Join(",", imagenes))}");
    }
}

public class TecnicoPerfilDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Rubro { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string FotoPortada { get; set; } = "";
    public string Logo { get; set; } = "";
    public string Instagram { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public bool EsPremium { get; set; }
}

public class PublicacionItem
{
    public int Id { get; set; }
    public int TecnicoId { get; set; }
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<string> Imagenes { get; set; } = new();
    public string PrimeraImagen => Imagenes.Count > 0
        ? "http://192.168.100.9:5191" + Imagenes[0]
        : "";
}