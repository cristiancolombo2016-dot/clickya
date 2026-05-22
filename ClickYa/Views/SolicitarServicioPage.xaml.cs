using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using System.Net.Http;

namespace ClickYa.Views;

[QueryProperty(nameof(Rubro), "rubro")]
public partial class SolicitarServicioPage : ContentPage
{
    private const string BASE_URL = "http://192.168.100.9:5191";
    private FileResult? _fotoSeleccionada;

    public string Rubro
    {
        set => LblRubro.Text = value;
    }

    public SolicitarServicioPage()
    {
        InitializeComponent();
    }

    private async void OnVolverTapped(object sender, EventArgs e)
        => await Navigation.PopAsync();

    private async void OnTomarFotoTapped(object sender, EventArgs e)
    {
        try
        {
            var accion = await DisplayActionSheet("Foto del problema", "Cancelar", null,
                "Tomar foto", "Elegir de galería");
            if (accion == "Tomar foto")
                _fotoSeleccionada = await MediaPicker.CapturePhotoAsync();
            else if (accion == "Elegir de galería")
                _fotoSeleccionada = await MediaPicker.PickPhotoAsync();

            if (_fotoSeleccionada != null)
            {
                ImgFoto.Source = ImageSource.FromFile(_fotoSeleccionada.FullPath);
                ImgFoto.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error foto: " + ex.Message);
        }
    }

    private async void OnEnviarTapped(object sender, EventArgs e)
    {
        var descripcion = EditorDescripcion.Text?.Trim() ?? "";
        var whatsapp = EntryWhatsApp.Text?.Trim() ?? "";
        var rubro = LblRubro.Text ?? "";

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            await DisplayAlert("Error", "Describí el problema antes de enviar", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(whatsapp))
        {
            await DisplayAlert("Error", "Ingresá tu número de WhatsApp", "OK");
            return;
        }

        try
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync(
                $"{BASE_URL}/api/Tecnico/categoria/{Uri.EscapeDataString(rubro)}");

            var opciones = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tecnicos = System.Text.Json.JsonSerializer.Deserialize<List<TecnicoUrgencia>>(json, opciones) ?? new();
            var premiums = tecnicos.Where(t => t.EsPremium).ToList();

            if (premiums.Count == 0)
            {
                await DisplayAlert("Sin disponibles",
                    "No hay técnicos premium disponibles para este rubro ahora.", "OK");
                return;
            }

            string? fotoUrl = null;

            if (_fotoSeleccionada != null)
            {
                try
                {
                    using var formData = new MultipartFormDataContent();
                    var stream = await _fotoSeleccionada.OpenReadAsync();
                    formData.Add(new StreamContent(stream), "archivo", _fotoSeleccionada.FileName);
                    var uploadRes = await http.PostAsync($"{BASE_URL}/api/Urgencia/foto", formData);
                    if (uploadRes.IsSuccessStatusCode)
                    {
                        var json2 = await uploadRes.Content.ReadAsStringAsync();
                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json2);
                        fotoUrl = $"{BASE_URL}{data?["url"]}";
                    }
                }
                catch { }
            }

            foreach (var tecnico in premiums)
            {
                var mensaje = $"Hola {tecnico.Nombre}! Tengo una urgencia de {rubro}.\n" +
                              $"Descripción: {descripcion}\n" +
                              $"Mi WhatsApp: {whatsapp}";

                if (fotoUrl != null)
                    mensaje += $"\nFoto del problema: {fotoUrl}";

                var url = $"https://wa.me/54{tecnico.WhatsApp}?text={Uri.EscapeDataString(mensaje)}";
                await Launcher.OpenAsync(url);
            }
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error urgencia: " + ex.Message);
            await DisplayAlert("Error", "Sin conexión con el servidor.", "OK");
        }
    }
}

public class TecnicoUrgencia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Rubro { get; set; } = "";
    public bool EsPremium { get; set; }
}