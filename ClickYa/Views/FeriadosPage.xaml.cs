using System.Net.Http.Json;

namespace ClickYa.Views;

public partial class FeriadosPage : ContentPage
{
    public FeriadosPage()
    {
        InitializeComponent();
        _ = CargarFeriados();
    }

    private async Task CargarFeriados()
    {
        try
        {
            using var http = new HttpClient();
            var lista = await http.GetFromJsonAsync<List<FeriadoApi>>(
                "https://date.nager.at/api/v3/PublicHolidays/2026/AR");

            if (lista == null) return;

            var hoy = DateTime.Today;
            var proximo = lista.FirstOrDefault(f => DateTime.Parse(f.Date) >= hoy);

            var items = lista.Select(f =>
            {
                var fecha = DateTime.Parse(f.Date);
                var esHoy = fecha.Date == hoy;
                var esProximo = f == proximo && !esHoy;

                return new FeriadoVM
                {
                    Dia = fecha.Day.ToString(),
                    Mes = fecha.ToString("MMM").ToUpper(),
                    DiaSemana = fecha.ToString("ddd").ToUpper(),
                    Nombre = f.LocalName,
                    Tipo = f.Types?.FirstOrDefault() == "Public" ? "Inamovible" :
                           f.Types?.FirstOrDefault() == "Optional" ? "No laborable" : "Puente",
                    Badge = esHoy ? "HOY" : esProximo ? "PRÓXIMO" : "",
                    TieneBadge = esHoy || esProximo,
                    Color = esHoy ? "#5C0BBF" : esProximo ? "#EDE9FE" : "White",
                    TextColor = esHoy ? "White" : "#222"
                };
            }).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ListaFeriados.ItemsSource = items;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error feriados: " + ex.Message);
        }
    }
}

public class FeriadoApi
{
    public string Date { get; set; } = "";
    public string LocalName { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string>? Types { get; set; }
}

public class FeriadoVM
{
    public string Dia { get; set; } = "";
    public string Mes { get; set; } = "";
    public string DiaSemana { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Badge { get; set; } = "";
    public bool TieneBadge { get; set; }
    public string Color { get; set; } = "White";
    public string TextColor { get; set; } = "#222";
}