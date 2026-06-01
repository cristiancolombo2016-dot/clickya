using System.Timers;
using System.Net.Http.Json;
using Microsoft.Maui.ApplicationModel;
using ClickYa.Services;
using ClickYa.Models;

namespace ClickYa.Views;

public partial class BaresPage : ContentPage
{
    private System.Timers.Timer bannerTimer;
    private readonly ComerciosService _comercioService = new();
    private const string BASE_URL = "https://clickya-production.up.railway.app";

    public BaresPage()
    {
        InitializeComponent();
        _ = CargarDesdeApi();
        IniciarCarrusel();
    }

    private async Task CargarDesdeApi()
    {
        // BANNERS
        try
        {
            using var http = new System.Net.Http.HttpClient();
            var banners = await http.GetFromJsonAsync<List<BannerDto>>(
                $"{BASE_URL}/api/Banners/seccion/bares");
            if (banners != null && banners.Count > 0)
            {
                var urls = banners.Select(b => $"{BASE_URL}{b.ImagenUrl}").ToList();
                MainThread.BeginInvokeOnMainThread(() =>
                    BannerCarousel.ItemsSource = urls);
            }
            else CargarBannersMock();
        }
        catch { CargarBannersMock(); }

        // LOCALES
        List<Local> locales = new();
        try
        {
            var l1 = await _comercioService.ObtenerPorRubroAsync("bares");
            if (l1 != null) locales.AddRange(l1);
            var l2 = await _comercioService.ObtenerPorRubroAsync("Bares");
            if (l2 != null) locales.AddRange(l2);
            locales = locales.DistinctBy(l => l.id).ToList();
        }
        catch { }

        var items = locales.Select(l => new
        {
            l.id,
            l.nombre,
            l.categoria,
            distancia = l.distancia ?? "",
            tieneDistancia = !string.IsNullOrWhiteSpace(l.distancia),
            Logo = string.IsNullOrWhiteSpace(l.logoUrl) ? "bar1.png"
                : l.logoUrl.StartsWith("http") ? l.logoUrl
                : $"{BASE_URL}{l.logoUrl}"
        }).ToList();

        // DESTACADOS
        List<object> destacados = new();
        try
        {
            using var httpDest = new System.Net.Http.HttpClient();
            var bannersDest = await httpDest.GetFromJsonAsync<List<BannerDto>>(
                $"{BASE_URL}/api/Banners/seccion/bares-destacados");

            if (bannersDest != null && bannersDest.Count > 0)
            {
                foreach (var b in bannersDest)
                {
                    var local = locales.FirstOrDefault(l => l.id == b.LocalId);
                    if (local != null)
                        destacados.Add(new
                        {
                            local.id,
                            local.nombre,
                            local.categoria,
                            Logo = $"{BASE_URL}{b.ImagenUrl}"
                        });
                }
            }
        }
        catch { }

        // PROMOS
        List<BannerPromoItem> promos = new();
        try
        {
            using var httpPromo = new System.Net.Http.HttpClient();
            var bannersPromo = await httpPromo.GetFromJsonAsync<List<BannerDto>>(
                $"{BASE_URL}/api/Banners/seccion/bares-promos");

            if (bannersPromo != null && bannersPromo.Count > 0)
            {
                promos = bannersPromo.Select(b => new BannerPromoItem
                {
                    LocalId = b.LocalId,
                    ImagenCompleta = $"{BASE_URL}{b.ImagenUrl}"
                }).ToList();
            }
        }
        catch { }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ListaDestacados.ItemsSource = destacados;
            ListaBares.ItemsSource = items;
            ListaPromos.ItemsSource = promos;
        });
    }

    void CargarBannersMock()
    {
        BannerCarousel.ItemsSource = new List<string> { "bar7.png", "bar8.png", "bar9.png" };
    }

    void IniciarCarrusel()
    {
        bannerTimer = new System.Timers.Timer(2000);
        bannerTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (BannerCarousel.ItemsSource is List<string> items && items.Count > 0)
                    BannerCarousel.Position = (BannerCarousel.Position + 1) % items.Count;
            });
        };
        bannerTimer.Start();
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

    private async void AbrirBuscador_Tapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("buscador");

    private async void OnPromoTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not BannerPromoItem banner) return;
        if (banner.LocalId <= 0) return;
        await Shell.Current.GoToAsync($"{nameof(BarPage)}?id={banner.LocalId}");
    }

    async void OnBarTapped(object sender, EventArgs e)
    {
        if (e is not TappedEventArgs tapped) return;
        var param = tapped.Parameter?.ToString();
        if (!string.IsNullOrWhiteSpace(param) && int.TryParse(param, out int id))
            await Shell.Current.GoToAsync($"{nameof(BarPage)}?id={id}");
    }
}