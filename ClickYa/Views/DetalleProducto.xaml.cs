using ClickYa.Services;
using ClickYa.Models;

namespace ClickYa.Views;

[QueryProperty(nameof(Producto), "producto")]
public partial class DetalleProducto : ContentPage
{
    private Articulo? _producto;

    public Articulo Producto
    {
        get => _producto!;
        set
        {
            _producto = value;
            CargarProducto();
        }
    }

    public DetalleProducto()
    {
        InitializeComponent();
    }

    private void CargarProducto()
    {
        if (_producto == null)
            return;

        ImgProducto.Source = _producto.imagen;
        LblNombre.Text = _producto.nombre;
        LblPrecio.Text = $"${_producto.precio}";
        LblDescripcion.Text = "Descripción del producto… (desde JSON próximamente)";
    }

    // BOTÓN WHATSAPP
    private void BtnWhatsApp_Clicked(object sender, EventArgs e)
    {
        string url = "https://wa.me/5493364000000";
        Launcher.OpenAsync(url);
    }

    // BOTÓN AGREGAR AL CARRITO
    private async void BtnAgregarCarrito_Clicked(object sender, EventArgs e)
    {
        if (_producto == null)
            return;

        // AGREGA AL CARRITO USANDO TU SERVICIO REAL
        CarritoService.Instancia.Agregar(_producto);

        await DisplayAlert("Carrito", $"{_producto.nombre} agregado al carrito.", "OK");

        // IR A LA PÁGINA DEL CARRITO (cuando exista)
        await Shell.Current.GoToAsync("carrito");
    }
}
