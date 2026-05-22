using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace ClickYa.Views;

public partial class ProfesionalPage : ContentPage
{
    public ProfesionalPage()
    {
        InitializeComponent();
    }

    // =========================
    // TAP EN IMÁGENES (PUBLICACIONES)
    // =========================
    async void OnImageTapped(object sender, EventArgs e)
    {
        if (sender is not Image img)
            return;

        if (img.GestureRecognizers.Count == 0)
            return;

        if (img.GestureRecognizers[0] is not TapGestureRecognizer tap ||
            tap.CommandParameter is not string param)
            return;

        var parts = param.Split('|');
        if (parts.Length != 2)
            return;

        string group = parts[0];
        if (!int.TryParse(parts[1], out int index))
            return;

        // MOCK – mañana esto viene de JSON
        List<string> images = group switch
        {
            "logo" => new() { "logo_clickya.png" },
            "pub1" => new() { "banner1.png", "banner2.png", "banner3.png" },
            "pub2" => new() { "paleta_frutilla.png", "paleta_chocolate.png", "helado_pote.png" },
            "pub3" => new() { "ropadeport.png", "tecnico.png", "automotores.png" },
            _ => new()
        };

        await Navigation.PushModalAsync(
            new ImageViewerPage(images, index)
        );
    }

    // =========================
    // BOTONES DE CONTACTO
    // =========================
    async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        string telefono = "5493364123456"; // ejemplo
        string mensaje = "Hola, vi tu perfil en ClickYa";

        string url = $"https://wa.me/{telefono}?text={Uri.EscapeDataString(mensaje)}";
        await Launcher.OpenAsync(url);
    }

    async void OnInstagramClicked(object sender, EventArgs e)
    {
        string usuario = "clickya.sn"; // ejemplo
        string url = $"https://www.instagram.com/{usuario}/";

        await Launcher.OpenAsync(url);
    }

    async void OnUbicacionClicked(object sender, EventArgs e)
    {
        string direccion = "San Nicolás de los Arroyos, Buenos Aires";
        string url = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(direccion)}";

        await Launcher.OpenAsync(url);
    }

   

}
