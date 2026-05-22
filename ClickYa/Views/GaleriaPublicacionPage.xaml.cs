namespace ClickYa.Views;

[QueryProperty(nameof(Titulo), "titulo")]
[QueryProperty(nameof(Imagenes), "imgs")]
public partial class GaleriaPublicacionPage : ContentPage
{
    private List<string> _imagenes = new();

    public string Titulo
    {
        set => LblTitulo.Text = value;
    }

    public string Imagenes
    {
        set
        {
            _imagenes = value.Split(',').ToList();
            CarruselFotos.ItemsSource = _imagenes;
            ActualizarContador(0);
        }
    }

    public GaleriaPublicacionPage()
    {
        InitializeComponent();
    }

    private void OnPosicionCambiada(object sender, PositionChangedEventArgs e)
    {
        ActualizarContador(e.CurrentPosition);
    }

    private void ActualizarContador(int pos)
    {
        LblContador.Text = $"{pos + 1} / {_imagenes.Count}";
    }

    private async void OnVolverTapped(object sender, EventArgs e)
        => await Navigation.PopAsync();
}