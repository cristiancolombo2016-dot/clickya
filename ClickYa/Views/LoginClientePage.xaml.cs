using Microsoft.Maui.Controls;
using System.Net.Http.Json;
namespace ClickYa.Views;
public partial class LoginClientePage : ContentPage
{
    private bool _passwordVisible = false;
    private const string API = "https://clickya-production.up.railway.app";
    public LoginClientePage()
    {
        InitializeComponent();
        ActualizarIconoPassword();
    }
    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        PasswordEntry.IsPassword = !_passwordVisible;
        ActualizarIconoPassword();
    }
    private void ActualizarIconoPassword()
    {
        TogglePasswordIcon.Source = new FontImageSource
        {
            Glyph = _passwordVisible ? "\uE8F4" : "\uE8F5",
            FontFamily = "MaterialIcons",
            Size = 22,
            Color = Color.FromArgb("#6A1B9A")
        };
    }
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Ingresá tu email y contraseña.", "OK");
            return;
        }
        try
        {
            using var http = new HttpClient();
            var response = await http.PostAsJsonAsync($"{API}/api/solicitudes/login", new
            {
                Email = email,
                Password = password
            });
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (data != null && !string.IsNullOrEmpty(data.DashboardTicket))
                {
                    var dashboardUrl = $"https://alert-kindness-production-90e4.up.railway.app/Comercio/Dashboard?ticket={Uri.EscapeDataString(data.DashboardTicket)}";
                    await Launcher.OpenAsync(dashboardUrl);
                }
            }
            else
            {
                await DisplayAlert("Error", "Email o contraseña incorrectos.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegistroClientePage));
    }
}
public class LoginResponse
{
    public string DashboardTicket { get; set; } = "";
    public int ComercioId { get; set; }
    public string Nombre { get; set; } = "";
}
