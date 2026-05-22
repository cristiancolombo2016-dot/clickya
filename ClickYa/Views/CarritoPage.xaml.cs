using ClickYa.Services;
using System.Linq;
using ClickYa.Models;

namespace ClickYa.Views
{
    public partial class CarritoPage : ContentPage
    {
        public CarritoPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Enlazamos el carrito REAL
            ListaCarrito.ItemsSource = CarritoService.Instancia.Items;
            ActualizarTotal();
        }

        // ============================
        // TOTAL
        // ============================
        private void ActualizarTotal()
        {
            int total = 0;

            foreach (var item in CarritoService.Instancia.Items)
            {
                total += item.precio * item.cantidad;
            }

            LblTotal.Text = $"Total: ${total}";
        }

        // ============================
        // BOTÓN SUMAR (+)
        // ============================
        private void BtnSumar_Clicked(object sender, EventArgs e)
        {
            if (sender is not Label lbl)
                return;

            if (lbl.BindingContext is not Articulo item)
                return;

            item.cantidad++;

            // Fuerzo refresco visual (como aún no usamos INotifyPropertyChanged)
            ListaCarrito.ItemsSource = null;
            ListaCarrito.ItemsSource = CarritoService.Instancia.Items;

            ActualizarTotal();
        }

        // ============================
        // BOTÓN RESTAR (-)
        // ============================
        private void BtnRestar_Clicked(object sender, EventArgs e)
        {
            if (sender is not Label lbl)
                return;

            if (lbl.BindingContext is not Articulo item)
                return;

            item.cantidad--;

            if (item.cantidad <= 0)
            {
                // Si llega a 0 lo sacamos del carrito
                CarritoService.Instancia.Quitar(item);
            }

            ListaCarrito.ItemsSource = null;
            ListaCarrito.ItemsSource = CarritoService.Instancia.Items;

            ActualizarTotal();
        }

        // ============================
        // ENVIAR POR WHATSAPP
        // ============================
        private async void BtnEnviarWhatsApp_Clicked(object sender, EventArgs e)
        {
            if (!CarritoService.Instancia.Items.Any())
            {
                await DisplayAlert("Carrito vacío", "No hay productos en el carrito.", "OK");
                return;
            }

            string mensaje = "Hola, quiero hacer este pedido:\n\n";

            foreach (var item in CarritoService.Instancia.Items)
            {
                mensaje += $"• {item.nombre} x{item.cantidad} - ${item.precio * item.cantidad}\n";
            }

            int total = CarritoService.Instancia.Items.Sum(i => i.precio * i.cantidad);
            mensaje += $"\nTOTAL: ${total}";

            string url = $"https://wa.me/5493364000000?text={Uri.EscapeDataString(mensaje)}";

            await Launcher.OpenAsync(url);
        }
    }
}
