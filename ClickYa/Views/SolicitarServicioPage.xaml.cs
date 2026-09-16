using System.Net.Http.Json;
using ClickYa.Services;
using Microsoft.Maui.Media;

namespace ClickYa.Views;

public partial class SolicitarServicioPage : ContentPage
{
    private const string BaseUrl = "https://clickya-production.up.railway.app";
    private FileResult? _fotoSeleccionada;

    public SolicitarServicioPage()
    {
        InitializeComponent();
        _ = CargarCategorias();
    }

    private async Task CargarCategorias()
    {
        try
        {
            using var http = new HttpClient();
            var categorias = await http.GetFromJsonAsync<List<CategoriaUrgenciaDto>>(
                $"{BaseUrl}/api/Categorias/seccion/servicios") ?? new();
            PickerCategoria.ItemsSource = categorias.OrderBy(c => c.Orden).ToList();
        }
        catch
        {
            await DisplayAlert("Sin conexión", "No se pudieron cargar los rubros.", "OK");
        }
    }

    private async void OnVolverTapped(object sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnTomarFotoTapped(object sender, EventArgs e)
    {
        try
        {
            var accion = await DisplayActionSheet("Foto del problema", "Cancelar", null,
                "Tomar foto", "Elegir de galería");
            _fotoSeleccionada = accion switch
            {
                "Tomar foto" => await MediaPicker.CapturePhotoAsync(),
                "Elegir de galería" => await MediaPicker.PickPhotoAsync(),
                _ => _fotoSeleccionada
            };
            if (_fotoSeleccionada != null)
            {
                ImgFoto.Source = ImageSource.FromFile(_fotoSeleccionada.FullPath);
                ImgFoto.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error seleccionando foto: " + ex.Message);
            await DisplayAlert("Foto", "No se pudo abrir la cámara o galería.", "OK");
        }
    }

    private async void OnEnviarTapped(object sender, EventArgs e)
    {
        if (PickerCategoria.SelectedItem is not CategoriaUrgenciaDto categoria ||
            string.IsNullOrWhiteSpace(EditorDescripcion.Text) ||
            string.IsNullOrWhiteSpace(EntryZona.Text) ||
            string.IsNullOrWhiteSpace(EntryDireccion.Text) ||
            string.IsNullOrWhiteSpace(EntryContacto.Text))
        {
            await DisplayAlert("Datos incompletos", "Completá rubro, descripción, zona, dirección y contacto.", "OK");
            return;
        }

        BtnEnviar.IsEnabled = false;
        try
        {
            using var http = new HttpClient();
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(categoria.Id.ToString()), "CategoriaId");
            form.Add(new StringContent(EditorDescripcion.Text.Trim()), "Descripcion");
            form.Add(new StringContent(EntryZona.Text.Trim()), "ZonaBarrio");
            form.Add(new StringContent(EntryDireccion.Text.Trim()), "DireccionExacta");
            form.Add(new StringContent(EntryContacto.Text.Trim()), "ContactoCliente");

            if (_fotoSeleccionada != null)
            {
                var stream = await _fotoSeleccionada.OpenReadAsync();
                form.Add(new StreamContent(stream), "Foto", _fotoSeleccionada.FileName);
            }

            var response = await http.PostAsync($"{BaseUrl}/api/Urgencia", form);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("No se publicó", string.IsNullOrWhiteSpace(error)
                    ? "La API rechazó la solicitud." : error, "OK");
                return;
            }

            var creada = await response.Content.ReadFromJsonAsync<UrgenciaCreadaDto>();
            if (creada == null || creada.Id <= 0 || string.IsNullOrWhiteSpace(creada.Token))
            {
                await DisplayAlert("Error", "La API no devolvió un acceso válido para la solicitud.", "OK");
                return;
            }

            try
            {
                await SolicitudesLocalesStore.Guardar(new SolicitudLocal
                {
                    Id = creada.Id,
                    Token = creada.Token,
                    Rubro = categoria.Nombre,
                    Fecha = creada.Fecha
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("La urgencia se publicó, pero SecureStorage falló: " + ex.Message);
                await DisplayAlert("Urgencia publicada",
                    "La solicitud se creó, pero el dispositivo no pudo guardar el acceso seguro. No la publiques nuevamente; revisá SecureStorage antes de continuar.", "OK");
                return;
            }
            await DisplayAlert("Urgencia publicada", "Ya podés consultar las ofertas en Mis solicitudes.", "OK");
            await Shell.Current.GoToAsync("mis-solicitudes");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error publicando urgencia: " + ex.Message);
            await DisplayAlert("Sin conexión", "La urgencia no pudo publicarse. Intentá nuevamente.", "OK");
        }
        finally
        {
            BtnEnviar.IsEnabled = true;
        }
    }
}

public sealed class CategoriaUrgenciaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
}

public sealed class UrgenciaCreadaDto
{
    public int Id { get; set; }
    public string Token { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime Fecha { get; set; }
}
