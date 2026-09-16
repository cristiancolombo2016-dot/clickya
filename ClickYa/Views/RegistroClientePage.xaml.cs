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

    // Limpia el número y lo arma en formato WhatsApp Argentina: 549 + caracteristica + numero
    private string ArmarWhatsApp(string numeroIngresado)
    {
        // Saca todo lo que no sea dígito (espacios, guiones, paréntesis, +)
        var soloNumeros = new string(numeroIngresado.Where(char.IsDigit).ToArray());

        // Saca el 0 inicial si lo pusieron (ej: 0336...)
        if (soloNumeros.StartsWith("0"))
            soloNumeros = soloNumeros.Substring(1);

        // Saca el 15 si quedó después de la caracteristica (caso 336 15 xxxx)
        // (solo si el numero es largo y tiene 15 en la posición típica)
        // Por simplicidad: si empieza con 54 ya viene con país, lo dejamos
        if (soloNumeros.StartsWith("549"))
            return soloNumeros;
        if (soloNumeros.StartsWith("54"))
            return "549" + soloNumeros.Substring(2);

        // Caso normal: el comercio puso caracteristica + numero (ej: 3364612832)
        return "549" + soloNumeros;
    }

    private async void OnRubroChanged(object sender, EventArgs e)
    {
        if (RubroPicker.SelectedItem == null) return;

        var rubro = RubroPicker.SelectedItem.ToString()!.ToLower();
        CategoriaPicker.ItemsSource = null;
        CategoriaPicker.ItemDisplayBinding = new Binding(nameof(CategoriaItem.Nombre));
        CategoriaPicker.Title = "Cargando...";

        try
        {
            var client = new HttpClient { BaseAddress = new Uri("https://clickya-production.up.railway.app/") };
            var categorias = await client.GetFromJsonAsync<List<CategoriaItem>>($"api/Categorias/seccion/{rubro}");

            if (categorias != null && categorias.Count > 0)
                CategoriaPicker.ItemsSource = categorias;
            else
                CategoriaPicker.ItemsSource = Array.Empty<CategoriaItem>();
        }
        catch
        {
            CategoriaPicker.ItemsSource = Array.Empty<CategoriaItem>();
            await DisplayAlert("Error", "No se pudieron cargar las categorías.", "OK");
        }

        CategoriaPicker.Title = "Categoría";
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

        var client = new HttpClient { BaseAddress = new Uri("https://clickya-production.up.railway.app/") };

        try
        {
            var esServicio = RubroPicker.SelectedItem?.ToString() == "Servicios";
            var categoria = CategoriaPicker.SelectedItem as CategoriaItem;
            if (categoria == null)
            {
                await DisplayAlert("Datos incompletos", "Seleccioná una categoría válida.", "OK");
                return;
            }
            HttpResponseMessage response;

            if (esServicio)
            {
                var tecnico = new
                {
                    Nombre = NombreEntry.Text,
                    Rubro = categoria.Nombre,
                    CategoriaId = categoria.Id,
                    WhatsApp = WhatsappEntry.Text
                };
                response = await client.PostAsJsonAsync("api/Tecnico/registro", tecnico);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(EmailEntry.Text))
                {
                    await DisplayAlert("Datos incompletos", "Ingresá tu email.", "OK");
                    return;
                }
                if (string.IsNullOrWhiteSpace(PasswordEntry.Text) || PasswordEntry.Text.Length < 8)
                {
                    await DisplayAlert("Datos incompletos", "La contraseña debe tener al menos 8 caracteres.", "OK");
                    return;
                }

                var solicitud = new
                {
                    Nombre = NombreEntry.Text,
                    Rubro = RubroPicker.Items[RubroPicker.SelectedIndex],
                    Categoria = categoria.Nombre,
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
                    if (tecnicoCreado != null && !string.IsNullOrEmpty(tecnicoCreado.DashboardTicket))
                    {
                        var dashboardUrl = $"https://alert-kindness-production-90e4.up.railway.app/Tecnico/Dashboard?ticket={Uri.EscapeDataString(tecnicoCreado.DashboardTicket)}";
                        await Launcher.OpenAsync(dashboardUrl);
                    }
                }

                await DisplayAlert("¡Listo!", esServicio
                    ? "Registro exitoso. Abrimos tu panel con una sesión segura."
                    : "Registro exitoso. Ingresá con tu email y contraseña desde la app.", "OK");

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
    public string DashboardTicket { get; set; } = "";
}
