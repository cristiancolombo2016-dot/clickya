using ClickYa.Services;
using ClickYa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClickYa.Views;

public partial class HeladoModalPage : ContentPage
{
    private const string BASE_URL = "http://192.168.100.9:5191";
    private int _comercioId;
    private string _whatsApp;
    private int maxSabores = 3;
    private decimal _precioCuarto = 0;
    private decimal _precioMedio = 0;
    private decimal _precioKilo = 0;
    private List<string> _saboresDisponibles = new();
    private List<CheckBox> _checkboxes = new();

    public HeladoModalPage(int comercioId, string whatsApp)
    {
        InitializeComponent();
        _comercioId = comercioId;
        _whatsApp = whatsApp;
        _ = CargarDesdeApi();
    }

    private async Task CargarDesdeApi()
    {
        try
        {
            using var http = new HttpClient();
            var data = await http.GetFromJsonAsync<HeladeriaDto>(
                $"{BASE_URL}/api/Heladeria/{_comercioId}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null) return;

            _precioCuarto = data.PrecioCuarto;
            _precioMedio = data.PrecioMedio;
            _precioKilo = data.PrecioKilo;

            _saboresDisponibles = data.Sabores?
                .Where(s => s.Disponible)
                .Select(s => s.Nombre)
                .ToList() ?? new();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Actualizar labels de tamaño con precios
                ActualizarLabelTamano(CardCuarto, $"1/4 kg — ${_precioCuarto:N0}");
                ActualizarLabelTamano(CardMedio, $"1/2 kg — ${_precioMedio:N0}");
                ActualizarLabelTamano(CardKilo, $"1 kg — ${_precioKilo:N0}");

                // Generar sabores dinámicamente
                GenerarSabores();

                SeleccionarMedio(null, null);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error cargando heladería: " + ex.Message);
            await MainThread.InvokeOnMainThreadAsync(() => SeleccionarMedio(null, null));
        }
    }

    private void ActualizarLabelTamano(Frame card, string texto)
    {
        if (card?.Content is Label lbl)
            lbl.Text = texto;
    }

    private void GenerarSabores()
    {
        _checkboxes.Clear();
        ContenedorSabores.Children.Clear();

        foreach (var sabor in _saboresDisponibles)
        {
            var chk = new CheckBox { Color = Color.FromArgb("#4B0082") };
            chk.CheckedChanged += Sabor_Checked;
            _checkboxes.Add(chk);

            var lbl = new Label
            {
                Text = sabor,
                TextColor = Color.FromArgb("#333"),
                VerticalOptions = LayoutOptions.Center
            };

            var row = new Frame
            {
                CornerRadius = 14,
                Padding = new Thickness(14),
                BorderColor = Color.FromArgb("#E0E0E0"),
                BackgroundColor = Colors.White,
                Content = new HorizontalStackLayout
                {
                    Spacing = 12,
                    Children = { chk, lbl }
                }
            };

            ContenedorSabores.Children.Add(row);
        }
    }

    private async void Cerrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void SeleccionarCuarto(object sender, EventArgs e)
    {
        MarcarSeleccion(CardCuarto);
        maxSabores = 2;
        LimpiarSabores();
    }

    private void SeleccionarMedio(object sender, EventArgs e)
    {
        MarcarSeleccion(CardMedio);
        maxSabores = 3;
        LimpiarSabores();
    }

    private void SeleccionarKilo(object sender, EventArgs e)
    {
        MarcarSeleccion(CardKilo);
        maxSabores = 4;
        LimpiarSabores();
    }

    private void MarcarSeleccion(Frame seleccionado)
    {
        foreach (var card in new[] { CardCuarto, CardMedio, CardKilo })
        {
            card.BackgroundColor = Colors.White;
            card.BorderColor = Color.FromArgb("#E0E0E0");
        }
        seleccionado.BackgroundColor = Color.FromArgb("#E9F9EF");
        seleccionado.BorderColor = Color.FromArgb("#4CD964");
    }

    private void Sabor_Checked(object sender, CheckedChangedEventArgs e)
    {
        var seleccionados = ObtenerSaboresSeleccionados();
        if (seleccionados.Count > maxSabores)
        {
            ((CheckBox)sender).IsChecked = false;
            DisplayAlert("Límite", $"Podés elegir hasta {maxSabores} sabores", "OK");
        }
    }

    private void LimpiarSabores()
    {
        foreach (var chk in _checkboxes)
            chk.IsChecked = false;
    }

    private List<string> ObtenerSaboresSeleccionados()
    {
        var sabores = new List<string>();
        for (int i = 0; i < _checkboxes.Count && i < _saboresDisponibles.Count; i++)
        {
            if (_checkboxes[i].IsChecked)
                sabores.Add(_saboresDisponibles[i]);
        }
        return sabores;
    }

    private async void AgregarHelado_Clicked(object sender, EventArgs e)
    {
        var sabores = ObtenerSaboresSeleccionados();
        if (sabores.Count == 0)
        {
            await DisplayAlert("Error", "Elegí al menos un sabor", "OK");
            return;
        }

        decimal precio = maxSabores switch
        {
            2 => _precioCuarto,
            3 => _precioMedio,
            4 => _precioKilo,
            _ => 0
        };

        string tamano = maxSabores switch
        {
            2 => "1/4 kg",
            3 => "1/2 kg",
            4 => "1 kg",
            _ => ""
        };

        string msg = $"Hola! Quiero pedir un helado de {tamano} con los siguientes sabores: {string.Join(", ", sabores)}. Precio: ${precio:N0}";
        string url = $"https://wa.me/{_whatsApp}?text={Uri.EscapeDataString(msg)}";
        await Launcher.OpenAsync(url);
        await Navigation.PopModalAsync();
    }
}

public class HeladeriaDto
{
    public int ComercioId { get; set; }
    public List<SaborDto> Sabores { get; set; } = new();
    public decimal PrecioCuarto { get; set; }
    public decimal PrecioMedio { get; set; }
    public decimal PrecioKilo { get; set; }
}

public class SaborDto
{
    public string Nombre { get; set; } = "";
    public bool Disponible { get; set; }
}
