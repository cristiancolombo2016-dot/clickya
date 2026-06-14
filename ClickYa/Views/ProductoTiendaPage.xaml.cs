using ClickYa.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClickYa.Views
{
    [QueryProperty(nameof(Producto), "producto")]
    public partial class ProductoTiendaPage : ContentPage
    {
        private ProductoTienda? _producto;

        public ProductoTienda? Producto
        {
            get => _producto;
            set
            {
                _producto = value;
                if (_producto != null)
                {
                    BindingContext = _producto;
                }
            }
        }

        public ProductoTiendaPage()
        {
            InitializeComponent();

            if (BindingContext == null)
            {
                BindingContext = new ProductoTienda
                {
                    Nombre = "Producto de prueba",
                    Precio = 15000,
                    Descripcion = "Descripción de ejemplo para diseño",
                    Imagenes = new()
                    {
                        "banner1.png"
                    }
                };
            }
        }

        private async void OnImagenTapped(object sender, EventArgs e)
        {
            if (BindingContext is not ProductoTienda producto) return;
            if (producto.Imagenes == null || producto.Imagenes.Count == 0) return;

            var stack = new HorizontalStackLayout { Spacing = 0, BackgroundColor = Colors.Black };

            foreach (var url in producto.Imagenes)
            {
                var img = new Image
                {
                    Source = url,
                    Aspect = Aspect.AspectFit,
                    WidthRequest = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density,
                    HeightRequest = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density,
                };
                stack.Children.Add(img);
            }

            var scroll = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                BackgroundColor = Colors.Black,
                Content = stack
            };

            var btnCerrar = new Button
            {
                Text = "✕",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.White,
                FontSize = 24,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 40, 16, 0)
            };

            var grid = new Grid { BackgroundColor = Colors.Black };
            grid.Children.Add(scroll);
            grid.Children.Add(btnCerrar);

            var modalPage = new ContentPage { BackgroundColor = Colors.Black };
            btnCerrar.Clicked += async (s, ev) => await Navigation.PopModalAsync();
            modalPage.Content = grid;

            await Navigation.PushModalAsync(modalPage);
        }

        private async void OnWhatsAppClicked(object sender, EventArgs e)
        {
            if (BindingContext is not ProductoTienda producto)
                return;

            // Si la tienda no tiene WhatsApp cargado, avisamos y no mandamos nada
            if (string.IsNullOrWhiteSpace(producto.WhatsAppTienda))
            {
                await DisplayAlert("Sin WhatsApp",
                    "Este comercio todavía no cargó su WhatsApp. Probá contactándolo desde su perfil.",
                    "OK");
                return;
            }

            // Armamos el mensaje con el nombre de la tienda
            string saludo = string.IsNullOrWhiteSpace(producto.NombreTienda)
                ? $"Hola! Quiero consultar por {producto.Nombre}"
                : $"Hola {producto.NombreTienda}! Quiero consultar por {producto.Nombre}";

            // Limpiamos el número y armamos el formato WhatsApp Argentina: 549 + numero
            var soloNumeros = new string(producto.WhatsAppTienda.Where(char.IsDigit).ToArray());
            if (soloNumeros.StartsWith("0"))
                soloNumeros = soloNumeros.Substring(1);
            string numeroFinal;
            if (soloNumeros.StartsWith("549"))
                numeroFinal = soloNumeros;
            else if (soloNumeros.StartsWith("54"))
                numeroFinal = "549" + soloNumeros.Substring(2);
            else
                numeroFinal = "549" + soloNumeros;

            string url = $"https://wa.me/{numeroFinal}?text={Uri.EscapeDataString(saludo)}";
            await Launcher.OpenAsync(url);
        }
    }
}
