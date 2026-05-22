using Microsoft.Maui.Controls;
using System.Net.Http.Json;

namespace ClickYa.Views;

public partial class RegistroClientePage : ContentPage
{
    public RegistroClientePage()
    {
        InitializeComponent();
        RubroPicker.SelectedIndexChanged += OnRubroChanged;
    }

    private void OnRubroChanged(object sender, EventArgs e)
    {
        if (RubroPicker.SelectedItem == null) return;
        var rubro = RubroPicker.SelectedItem.ToString();
        CategoriaPicker.ItemsSource = null;

        if (rubro == "Comidas")
            CategoriaPicker.ItemsSource = new List<string> { "pizzeria", "hamburguesa", "parrilla", "heladeria", "cafeteria" };
        else if (rubro == "Tiendas")
            CategoriaPicker.ItemsSource = new List<string> { "tecnologia", "ropa", "calzado", "hogar", "belleza", "salud", "ferreterias", "autos y motos", "deportes", "otros rubros" };
        else if (rubro == "Servicios")
            CategoriaPicker.ItemsSource = new List<string> { "electricista", "plomero", "gasista", "tecnico" };
        else if (rubro == "Bares")
            CategoriaPicker.ItemsSource = new List<string> { "cerveceria", "bar", "pub" };

        CategoriaPicker.SelectedItem = null;
    }

    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
            RubroPicker.SelectedItem == null ||
            CategoriaPicker.SelectedItem == null ||
            string.IsNullOrWhiteSpace(WhatsappEntry.Text))
        {
            await DisplayAlert("Datos incompletos", "Completá el nombre, el rubro, la categoría y el WhatsApp.", "OK");
            return;
        }

        var client = new HttpClient { BaseAddress = new Uri("http://192.168.100.9:5191/") };

        try
        {
            var esServicio = RubroPicker.SelectedItem?.ToString() == "Servicios";
            HttpResponseMessage response;

            if (esServicio)
            {
                var tecnico = new
                {
                    Nombre = NombreEntry.Text,
                    Rubro = CategoriaPicker.SelectedItem?.ToString() ?? "",
                    WhatsApp = WhatsappEntry.Text,
                    Token = Guid.NewGuid().ToString("N"),
                    Activo = true,
                    EsPremium = false
                };
                response = await client.PostAsJsonAsync("api/Tecnico", tecnico);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(EmailEntry.Text))
                {
                    await DisplayAlert("Datos incompletos", "Ingresá tu email.", "OK");
                    return;
                }
                if (string.IsNullOrWhiteSpace(PasswordEntry.Text) || PasswordEntry.Text.Length < 6)
                {
                    await DisplayAlert("Datos incompletos", "La contraseña debe tener al menos 6 caracteres.", "OK");
                    return;
                }

                var solicitud = new
                {
                    Nombre = NombreEntry.Text,
                    Rubro = RubroPicker.Items[RubroPicker.SelectedIndex],
                    Categoria = CategoriaPicker.Items[CategoriaPicker.SelectedIndex],
                    Telefono = WhatsappEntry.Text,
                    Descripcion = string.IsNullOrWhiteSpace(MensajeEditor.Text) ? "Sin descripción" : MensajeEditor.Text,
                    Email = EmailEntry.Text.Trim(),
                    Password = PasswordEntry.Text
                };
                response = await client.PostAsJsonAsync("api/solicitudes", solicitud);
            }

            if (response.IsSuccessStatusCode)
            {
                if (esServicio)
                {
                    var tecnicoCreado = await response.Content.ReadFromJsonAsync<TecnicoRegistrado>();
                    if (tecnicoCreado != null && !string.IsNullOrEmpty(tecnicoCreado.Token))
                    {
                        var dashboardUrl = $"http://192.168.100.9:5175/Tecnico/Dashboard?token={tecnicoCreado.Token}";
                        var mensaje = $"Hola {tecnicoCreado.Nombre}! 👋 Bienvenido a ClickYa.\n\nTu panel de control es este link, guardalo:\n\n{dashboardUrl}";
                        var waUrl = $"https://wa.me/54{WhatsappEntry.Text.Trim()}?text={Uri.EscapeDataString(mensaje)}";
                        await Launcher.OpenAsync(waUrl);
                    }
                }
                else
                {
                    var comercioCreado = await response.Content.ReadFromJsonAsync<ComercioRegistrado>();
                    if (comercioCreado != null && !string.IsNullOrEmpty(comercioCreado.Token))
                    {
                        var dashboardUrl = $"http://192.168.100.9:5175/Comercio/Dashboard?token={comercioCreado.Token}";
                        var mensaje = $"Hola {comercioCreado.Nombre}! 👋 Bienvenido a ClickYa.\n\nTu panel de control:\n{dashboardUrl}\n\nEntrá con tu email y contraseña desde la app.";
                        var waUrl = $"https://wa.me/54{WhatsappEntry.Text.Trim()}?text={Uri.EscapeDataString(mensaje)}";
                        await Launcher.OpenAsync(waUrl);
                    }
                }

                await DisplayAlert("¡Listo!", esServicio
                    ? "Registro exitoso. Guardá el link que te enviamos por WhatsApp."
                    : "Registro exitoso. Te enviamos el acceso por WhatsApp.", "OK");

                NombreEntry.Text = string.Empty;
                WhatsappEntry.Text = string.Empty;
                EmailEntry.Text = string.Empty;
                MensajeEditor.Text = string.Empty;
                RubroPicker.SelectedItem = null;
                CategoriaPicker.SelectedItem = null;
                if (PasswordEntry != null) PasswordEntry.Text = string.Empty;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", error, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Excepción", ex.Message, "OK");
        }
    }
}

public class TecnicoRegistrado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Token { get; set; } = "";
}

public class ComercioRegistrado
{
    public string Token { get; set; } = "";
    public int ComercioId { get; set; }
    public string Nombre { get; set; } = "";
}