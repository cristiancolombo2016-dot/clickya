using System.Text.Json;
namespace ClickYa.Views;
[QueryProperty(nameof(TecnicoId), "id")]
public partial class PerfilTecnicoPage : ContentPage
{
    private const string BASE_URL = "https://clickya-production.up.railway.app";
    private string _whatsApp = "";
    private string _instagram = "";
    private string _ubicacion = "";
    private int _idTecnico = 0;
    private string _nombreTecnico = "";
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
            _instagram = tecnico.Instagram ?? "";
            _ubicacion = tecnico.Ubicacion ?? "";
            _idTecnico = tecnico.Id;
            _nombreTecnico = tecnico.Nombre;
            LblNombre.Text = tecnico.Nombre;
            LblRubro.Text = tecnico.Rubro;

            LblDireccion.Text = string.IsNullOrEmpty(tecnico.Direccion)
                ? "" : "📍 " + tecnico.Direccion;
            LblDescripcion.Text = tecnico.Descripcion;

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

        await Launcher.OpenAsync($"https://wa.me/{numeroFinal}");
    }
    private async void OnInstagramTapped(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_instagram))
        {
            await DisplayAlert("Instagram", "Este técnico no cargó su Instagram.", "OK");
            return;
        }

        var ig = _instagram.StartsWith("http")
            ? _instagram
            : $"https://instagram.com/{_instagram.Replace("@", "").Trim()}";

        await Launcher.OpenAsync(ig);
    }
    private async void OnUbicacionTapped(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ubicacion))
        {
            await DisplayAlert("Ubicación", "Este técnico no cargó su ubicación.", "OK");
            return;
        }

        var ubicacion = _ubicacion;
        if (!ubicacion.StartsWith("http"))
            ubicacion = $"https://maps.google.com/?q={Uri.EscapeDataString(ubicacion)}";

        await Launcher.OpenAsync(ubicacion);
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
            .Select(i => "https://clickya-production.up.railway.app" + i)
            .ToList();

        await Shell.Current.GoToAsync(
            $"galeria-pub?titulo={Uri.EscapeDataString(pub.Titulo)}&imgs={Uri.EscapeDataString(string.Join(",", imagenes))}");
    }

    // ============================================
    // REPORTAR TÉCNICO
    // ============================================
    private async void OnReportarTapped(object sender, EventArgs e)
    {
        if (_idTecnico == 0) return;

        // Menú de opciones (estilo Instagram)
        string accion = await DisplayActionSheet(
            "Opciones", "Cancelar", null, "🚩 Reportar técnico");

        if (accion != "🚩 Reportar técnico") return;

        // Elegir el motivo del reporte
        string motivo = await DisplayActionSheet(
            "¿Por qué querés reportar este técnico?", "Cancelar", null,
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
                comercioId = _idTecnico,
                nombreComercio = _nombreTecnico,
                motivo = motivo,
                detalle = ""
            };

            var json = JsonSerializer.Serialize(reporte);
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
        ? "https://clickya-production.up.railway.app" + Imagenes[0]
        : "";
}