using ClickYa.Models;
using ClickYa.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Timers;

namespace ClickYa.Views
{
    public class BannerDto
    {
        public int Id { get; set; }
        public string Seccion { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public int LocalId { get; set; }
        public int TecnicoId { get; set; }
        public int Dias { get; set; }
    }

    public partial class HomePage : ContentPage
    {
        private System.Timers.Timer bannerTimer;
        private List<BannerDto> _bannersHome = new();

        private const string BASE_URL = "https://clickya-production.up.railway.app";

        public HomePage()
        {
            InitializeComponent();
            CargarHomeDesdeApi();
            _ = CargarProximoFeriado();
        }

        private void CargarDatosTemporales()
        {
            BannerCarousel.ItemsSource = new List<string>
            {
                "banner1.png",
                "banner2.png",
                "banner3.png",
                "banner4.png"
            };

            ListaDestacados.ItemsSource = new List<object>
            {
                new { Imagen = "pizza.png",       Nombre = "Pizza Especial",   Precio = "$4.500",             LocalId = 0 },
                new { Imagen = "ropa.png",        Nombre = "Camisa a Cuadros", Precio = "$7.200",             LocalId = 0 },
                new { Imagen = "ropadeporte.png", Nombre = "Ropa Deportiva",   Precio = "Ver más",            LocalId = 0 },
                new { Imagen = "automotores.png", Nombre = "Automotores",      Precio = "Ver más",            LocalId = 0 },
                new { Imagen = "tecnico.png",     Nombre = "Servicio Técnico", Precio = "Ver más",            LocalId = 0 },
                new { Imagen = "banner4.png",     Nombre = "Barbería",         Precio = "Turnos disponibles", LocalId = 0 },
            };
        }

        private async void CargarHomeDesdeApi()
        {
            try
            {
                using var http = new HttpClient();

                // BANNERS DESDE API
                try
                {
                    var banners = await http.GetFromJsonAsync<List<BannerDto>>(
                        $"{BASE_URL}/api/Banners/seccion/home");

                    if (banners != null && banners.Count > 0)
                    {
                        _bannersHome = banners;
                        var urls = banners.Select(b => $"{BASE_URL}{b.ImagenUrl}").ToList();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            BannerCarousel.ItemsSource = urls;
                            IniciarCarrusel();
                        });
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            BannerCarousel.ItemsSource = new List<string>
        {
            "banner1.png", "banner2.png",
            "banner3.png", "banner4.png"
        };
                            IniciarCarrusel();
                        });
                    }
                }
                catch
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        BannerCarousel.ItemsSource = new List<string>
        {
            "banner1.png", "banner2.png",
            "banner3.png", "banner4.png"
        };
                        IniciarCarrusel();
                    });
                }


                // DESTACADOS — banners de home-destacados
                try
                {
                    var bannersDest = await http.GetFromJsonAsync<List<BannerDto>>(
                        $"{BASE_URL}/api/Banners/seccion/home-destacados");

                    if (bannersDest != null && bannersDest.Count > 0)
                    {
                        var comerciosService = new ComerciosService();
                        var todosLocales = await comerciosService.ObtenerTodosAsync();

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ListaDestacados.ItemsSource = bannersDest
                                .Select(b =>
                                {
                                    var local = todosLocales?.FirstOrDefault(l => l.id == b.LocalId);
                                    return new
                                    {
                                        Imagen = $"{BASE_URL}{b.ImagenUrl}",
                                        Nombre = local?.nombre ?? "Local",
                                        Rating = "4.8",
                                        Ubicacion = local?.ubicacion ?? "San Nicolás",
                                        LocalId = b.LocalId
                                    };
                                })
                                .ToList<object>();
                        });
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ListaDestacados.ItemsSource = new List<object>();
                        });
                    }
                }
                catch
                {
                    ListaDestacados.ItemsSource = new List<object>();
                }


            }
            catch
            {
                CargarDatosTemporales();
                IniciarCarrusel();
            }
        }

        private void IniciarCarrusel()
        {
            bannerTimer?.Stop();
            bannerTimer?.Dispose();

            bannerTimer = new System.Timers.Timer(2500);
            bannerTimer.AutoReset = true;

            bannerTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Dispatch(() =>
                {
                    var items = BannerCarousel.ItemsSource as IEnumerable<string>;
                    if (items == null) return;

                    var list = items as List<string> ?? items.ToList();
                    if (list.Count < 2) return;

                    BannerCarousel.Position = (BannerCarousel.Position + 1) % list.Count;
                });
            };

            bannerTimer.Start();
        }

        private async void Banner_Tapped(object sender, EventArgs e)
        {
            int position = BannerCarousel.Position;

            if (_bannersHome.Count == 0 || position >= _bannersHome.Count)
                return;

            var banner = _bannersHome[position];

            if (banner.LocalId > 0)
                await Shell.Current.GoToAsync($"local?id={banner.LocalId}");
        }

        private async void AbrirComida_Tapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("locales");

        private async void AbrirTiendas_Tapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("tiendas-categorias");

        private async void AbrirServicios_Tapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("servicios-categorias");

        private async void AbrirEspectaculos_Tapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("bares");

        private async void AbrirBuscador_Tapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("buscador");

        private async void AbrirFarmacias_Tapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("farmacias-turno");
        }

        private async void AbrirCine_Tapped(object sender, TappedEventArgs e)
        {
            await DisplayAlert(
                "Cartelera",
                "Próximamente disponible",
                "OK");
        }
        private async void Destacado_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter == null) return;

            var tipo = e.Parameter.GetType();
            var prop = tipo.GetProperty("LocalId");
            if (prop == null) return;

            int localId = Convert.ToInt32(prop.GetValue(e.Parameter));
            if (localId <= 0) return;

            await Shell.Current.GoToAsync($"local?id={localId}");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            bannerTimer?.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            bannerTimer?.Stop();
        }

        private async Task CargarProximoFeriado()
        {
    try
    {
        using var http = new HttpClient();
        var lista = await http.GetFromJsonAsync<List<FeriadoApi>>(
            "https://date.nager.at/api/v3/PublicHolidays/2026/AR");

        if (lista == null) return;

        var hoy = DateTime.Today;
        var proximo = lista.FirstOrDefault(f => DateTime.Parse(f.Date) >= hoy);

        if (proximo == null) return;

        var fecha = DateTime.Parse(proximo.Date);
        var diasRestantes = (fecha - hoy).Days;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            LblFeriadoTitulo.Text = $"Próximo feriado: {fecha.Day} de {fecha.ToString("MMMM")}";
            LblFeriadoSub.Text = diasRestantes == 0 ? "¡Es hoy!" :
                                 diasRestantes == 1 ? "¡Es mañana!" :
                                 $"En {diasRestantes} días · {proximo.LocalName}";
        });
    }
    catch { }
}

        private async void AbrirFeriados_Tapped(object sender, TappedEventArgs e)
                    => await Shell.Current.GoToAsync("feriados");

    }  // cierre de HomePage

   

}  // cierre del namespace