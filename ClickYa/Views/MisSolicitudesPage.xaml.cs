using System.Net.Http.Json;
using ClickYa.Services;

namespace ClickYa.Views;

public partial class MisSolicitudesPage : ContentPage
{
    private const string BaseUrl = "https://clickya-production.up.railway.app";

    public MisSolicitudesPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Cargar();
    }

    private async Task Cargar()
    {
        var locales = await SolicitudesLocalesStore.Obtener();
        var items = new List<UrgenciaClienteDto>();
        using var http = new HttpClient();
        foreach (var local in locales.OrderByDescending(s => s.Fecha))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/Urgencia/{local.Id}/cliente");
                request.Headers.Add("X-ClickYa-Solicitud-Token", local.Token);
                using var response = await http.SendAsync(request);
                if (!response.IsSuccessStatusCode) continue;
                var detalle = await response.Content.ReadFromJsonAsync<UrgenciaClienteDto>();
                if (detalle == null) continue;
                detalle.Token = local.Token;
                foreach (var oferta in detalle.Ofertas)
                {
                    oferta.UrgenciaId = detalle.Id;
                    oferta.Token = local.Token;
                    oferta.UrgenciaSeleccionada = detalle.OfertaSeleccionadaId != null;
                }
                items.Add(detalle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error cargando solicitud: " + ex.Message);
            }
        }
        ListaSolicitudes.ItemsSource = items;
    }

    private async void Seleccionar_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: OfertaSolicitudDto oferta }) return;
        var confirmar = await DisplayAlert("Elegir técnico",
            $"¿Querés elegir la oferta de {oferta.TecnicoNombre}?", "Elegir", "Cancelar");
        if (!confirmar) return;
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}/api/Urgencia/{oferta.UrgenciaId}/seleccionar-oferta/{oferta.Id}");
        request.Headers.Add("X-ClickYa-Solicitud-Token", oferta.Token);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await DisplayAlert("No se pudo elegir", await response.Content.ReadAsStringAsync(), "OK");
            return;
        }
        await DisplayAlert("Oferta elegida", "El técnico ya puede ver tu dirección y contacto.", "OK");
        await Cargar();
    }

    private async void VerPerfil_Clicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: OfertaSolicitudDto oferta })
            await Shell.Current.GoToAsync($"perfil-tecnico?id={oferta.TecnicoId}");
    }

    private async void Calificar_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: UrgenciaClienteDto urgencia }) return;
        var seleccion = await DisplayActionSheet("Calificación", "Cancelar", null,
            "5 estrellas", "4 estrellas", "3 estrellas", "2 estrellas", "1 estrella");
        if (seleccion == null || seleccion == "Cancelar") return;
        var estrellas = int.Parse(seleccion[..1]);
        var comentario = await DisplayPromptAsync("Comentario", "Comentario opcional", "Enviar", "Omitir", maxLength: 500);
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}/api/Urgencia/{urgencia.Id}/calificacion");
        request.Headers.Add("X-ClickYa-Solicitud-Token", urgencia.Token);
        request.Content = JsonContent.Create(new { estrellas, comentario });
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await DisplayAlert("No se pudo calificar", await response.Content.ReadAsStringAsync(), "OK");
            return;
        }
        await DisplayAlert("Gracias", "Tu opinión fue registrada.", "OK");
        await Cargar();
    }

    private async void Volver_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}

public sealed class UrgenciaClienteDto
{
    public int Id { get; set; }
    public string Token { get; set; } = "";
    public string Rubro { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string ZonaBarrio { get; set; } = "";
    public string Estado { get; set; } = "";
    public int? OfertaSeleccionadaId { get; set; }
    public bool YaCalificada { get; set; }
    public List<OfertaSolicitudDto> Ofertas { get; set; } = new();
    public string ZonaTexto => "Zona: " + ZonaBarrio;
    public string OfertasTexto => $"{Ofertas.Count} oferta{(Ofertas.Count == 1 ? "" : "s")}";
    public bool PuedeCalificar => OfertaSeleccionadaId.HasValue && !YaCalificada;
}

public sealed class OfertaSolicitudDto
{
    public int Id { get; set; }
    public int UrgenciaId { get; set; }
    public string Token { get; set; } = "";
    public int TecnicoId { get; set; }
    public string TecnicoNombre { get; set; } = "";
    public decimal PrecioEstimado { get; set; }
    public string Disponibilidad { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public double PromedioEstrellas { get; set; }
    public int CantidadOpiniones { get; set; }
    public bool Seleccionada { get; set; }
    public bool UrgenciaSeleccionada { get; set; }
    public bool PuedeSeleccionar => !UrgenciaSeleccionada;
    public string PrecioTexto => $"${PrecioEstimado:N0}";
    public string ReputacionTexto => CantidadOpiniones == 0 ? "Sin opiniones"
        : $"⭐ {PromedioEstrellas:F1} ({CantidadOpiniones})";
}
