using ClickYa.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using ClickYa.Services;
using System.Linq;
using System.Text.Json;

namespace ClickYa.Views
{
    [QueryProperty(nameof(IdLocal), "id")]
    public partial class TiendaLocalPage : ContentPage
    {
        private int idLocal;

        public int IdLocal
        {
            get => idLocal;
            set
            {
                idLocal = value;
                CargarLocal();
            }
        }

        public TiendaLocalPage()
        {
            InitializeComponent();
        }

        private readonly ClickYaDataService _dataService = new();

        private async void CargarLocal()
        {
            if (idLocal == 0)
                idLocal = 1;

            var local = await _dataService.ObtenerLocalAsync(idLocal);
            var publicaciones = await _dataService.ObtenerPublicacionesAsync(idLocal);

            if (local == null)
                return;

            var productos = publicaciones?
                .Select(p =>
                {
                    var talles = new List<string>();
                    var colores = new List<string>();
                    var anio = "";
                    var colorAuto = "";
                    var zona = "";
                    var horario = "";
                    var ingredientes = "";

                    if (!string.IsNullOrWhiteSpace(p.DatosExtraJson))
                    {
                        try
                        {
                            var extra = JsonDocument.Parse(p.DatosExtraJson).RootElement;

                            if (extra.TryGetProperty("tallas", out var t))
                                talles = t.EnumerateArray()
                                          .Select(x => x.GetString() ?? "")
                                          .Where(x => !string.IsNullOrWhiteSpace(x))
                                          .ToList();

                            if (extra.TryGetProperty("colores", out var c) &&
                                c.GetString() is string col &&
                                !string.IsNullOrWhiteSpace(col))
                                colores = col.Split(',').Select(x => x.Trim()).ToList();

                            if (extra.TryGetProperty("anio", out var a))
                                anio = a.GetString() ?? "";

                            if (extra.TryGetProperty("color", out var ca))
                                colorAuto = ca.GetString() ?? "";

                            if (extra.TryGetProperty("zona", out var z))
                                zona = z.GetString() ?? "";

                            if (extra.TryGetProperty("horario", out var h))
                                horario = h.GetString() ?? "";

                            if (extra.TryGetProperty("ingredientes", out var i))
                                ingredientes = i.GetString() ?? "";
                        }
                        catch { }
                    }

                    return new ProductoTienda
                    {
                        Nombre = p.Titulo,
                        Precio = int.TryParse(p.Precio?.Replace("$", "").Replace(".", ""), out var pr) ? pr : 0,
                        Descripcion = p.Descripcion,
                        Rubro = p.Rubro,
                        Talles = talles,
                        Colores = colores,
                        Anio = anio,
                        ColorAuto = colorAuto,
                        Zona = zona,
                        Horario = horario,
                        Ingredientes = ingredientes,
                        Seccion = ExtraerSeccion(p.DatosExtraJson),
                        WhatsAppTienda = local.whatsApp ?? "",
                        NombreTienda = local.nombre ?? "",
                        Imagenes = p.ImagenesUrls != null && p.ImagenesUrls.Count > 0
                            ? p.ImagenesUrls.Select(url => url.StartsWith("http") ? url : _dataService.BaseUrl + url).ToList()
                            : new List<string> { string.IsNullOrWhiteSpace(p.ImagenUrl) ? "banner1.png" : (p.ImagenUrl.StartsWith("http") ? p.ImagenUrl : _dataService.BaseUrl + p.ImagenUrl) }
                    };
                })
                .ToList() ?? new List<ProductoTienda>();

            BindingContext = new
            {
                nombre = local.nombre,
                direccion = local.ubicacion,
                logo = string.IsNullOrWhiteSpace(local.logoUrl)
                    ? "logo_moda.png"
                    : _dataService.BaseUrl + local.logoUrl,
                whatsapp = local.whatsApp,
                instagram = local.instagram,
                ubicacion = local.ubicacion,
                productos = productos
    .GroupBy(p => string.IsNullOrWhiteSpace(p.Seccion) ? "Productos" : p.Seccion)
    .Select(g => new GrupoProductosTienda(g.Key, g.ToList()))
    .ToList()
            };

            if (!string.IsNullOrWhiteSpace(local.portadaUrl))
                ImgPortada.Source = _dataService.BaseUrl + local.portadaUrl;

            if (!string.IsNullOrWhiteSpace(local.logoUrl))
                ImgLogo.Source = _dataService.BaseUrl + local.logoUrl;
        }

        private async void OnProductoTapped(object sender, EventArgs e)
        {
            if (sender is not BindableObject b) return;
            if (b.BindingContext is not ProductoTienda producto) return;

            await Shell.Current.GoToAsync("producto-tienda", new Dictionary<string, object>
            {
                { "producto", producto }
            });
        }

        private async void OnLogoTapped(object sender, EventArgs e)
        {
            var logo = BindingContext?.GetType()
                .GetProperty("logo")?
                .GetValue(BindingContext)?
                .ToString();

            if (string.IsNullOrWhiteSpace(logo)) return;

            await Navigation.PushModalAsync(new ContentPage
            {
                BackgroundColor = Colors.Black,
                Content = new Image { Source = logo, Aspect = Aspect.AspectFit }
            });
        }

        private async void OnWhatsAppClicked(object sender, EventArgs e)
        {
            var telefono = BindingContext?.GetType()
                .GetProperty("whatsapp")?
                .GetValue(BindingContext)?
                .ToString();

            if (string.IsNullOrWhiteSpace(telefono)) return;

            // Limpia el número y arma el formato WhatsApp Argentina: 549 + caracteristica + numero
            var soloNumeros = new string(telefono.Where(char.IsDigit).ToArray());

            if (soloNumeros.StartsWith("0"))
                soloNumeros = soloNumeros.Substring(1);

            string numeroFinal;
            if (soloNumeros.StartsWith("549"))
                numeroFinal = soloNumeros;
            else if (soloNumeros.StartsWith("54"))
                numeroFinal = "549" + soloNumeros.Substring(2);
            else
                numeroFinal = "549" + soloNumeros;

            await Launcher.OpenAsync($"https://wa.me/{numeroFinal}");
        }

        private async void OnInstagramClicked(object sender, EventArgs e)
        {
            var insta = BindingContext?.GetType()
                .GetProperty("instagram")?
                .GetValue(BindingContext)?
                .ToString();

            if (string.IsNullOrWhiteSpace(insta))
            {
                await DisplayAlert("Instagram", "Este comercio no cargó su Instagram.", "OK");
                return;
            }

            if (!insta.StartsWith("http"))
                insta = $"https://instagram.com/{insta.Replace("@", "").Trim()}";

            await Launcher.OpenAsync(insta);
        }

        private async void OnUbicacionClicked(object sender, EventArgs e)
        {
            var direccion = BindingContext?.GetType()
                .GetProperty("ubicacion")?
                .GetValue(BindingContext)?
                .ToString();

            if (string.IsNullOrWhiteSpace(direccion)) return;

            await Launcher.OpenAsync(
                $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(direccion)}");
        }

        private async void OnCompartirClicked(object sender, EventArgs e)
        {
            var nombre = BindingContext?.GetType()
                .GetProperty("nombre")?
                .GetValue(BindingContext)?
                .ToString();

            var whatsapp = BindingContext?.GetType()
                .GetProperty("whatsapp")?
                .GetValue(BindingContext)?
                .ToString();

            await Share.RequestAsync(new ShareTextRequest
            {
                Title = nombre ?? "ClickYa",
                Text = $"Mirá {nombre} en ClickYa!\nWhatsApp: {whatsapp}"
            });
        }

        private string ExtraerSeccion(string? json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return "";
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("seccion", out var s))
                    return s.GetString() ?? "";
                return "";
            }
            catch { return ""; }
        }
        // ============================================
        // REPORTAR COMERCIO
        // ============================================
        private async void OnReportarTapped(object sender, EventArgs e)
        {
            if (idLocal == 0) return;

            var nombre = BindingContext?.GetType()
                .GetProperty("nombre")?
                .GetValue(BindingContext)?
                .ToString() ?? "";

            // Menú de opciones (estilo Instagram)
            string accion = await DisplayActionSheet(
                "Opciones", "Cancelar", null, "🚩 Reportar comercio");

            if (accion != "🚩 Reportar comercio") return;

            // Elegir el motivo del reporte
            string motivo = await DisplayActionSheet(
                "¿Por qué querés reportar este comercio?", "Cancelar", null,
                "Contenido inapropiado",
                "Información falsa o engañosa",
                "Estafa o fraude",
                "No existe / cerró",
                "Otro");

            if (string.IsNullOrWhiteSpace(motivo) || motivo == "Cancelar") return;

            // Enviar el reporte a la API
            try
            {
                var reporte = new
                {
                    comercioId = idLocal,
                    nombreComercio = nombre,
                    motivo = motivo,
                    detalle = ""
                };

                var json = System.Text.Json.JsonSerializer.Serialize(reporte);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var http = new HttpClient();
                var resp = await http.PostAsync($"{_dataService.BaseUrl}/api/Reportes", content);

                if (resp.IsSuccessStatusCode)
                    await DisplayAlert("Gracias", "Tu reporte fue enviado. Lo vamos a revisar.", "OK");
                else
                    await DisplayAlert("Error", "No se pudo enviar el reporte. Intentá de nuevo.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR reporte: " + ex.Message);
                await DisplayAlert("Error", "No se pudo enviar el reporte. Revisá tu conexión.", "OK");
            }
        }
    }  // cierre de TiendaLocalPage

}      // cierre del namespace

public class GrupoProductosTienda : List<ProductoTienda>
{
    public string Titulo { get; set; } = "";
    public GrupoProductosTienda(string titulo, List<ProductoTienda> items) : base(items)
    {
        Titulo = titulo;
    }
}