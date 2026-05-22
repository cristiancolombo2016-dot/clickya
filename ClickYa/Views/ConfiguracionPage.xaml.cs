using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using ClickYa.Services;
using System.Linq;

namespace ClickYa.Views;

public partial class ConfiguracionPage : ContentPage
{
    private readonly CiudadesService _service = new();
    private List<Ciudad> _ciudades = new();

    public ConfiguracionPage()
    {
        InitializeComponent();
        _ = CargarLocalidadesAsync();
    }

    private async Task CargarLocalidadesAsync()
    {
        LocalidadPicker.Items.Clear();

        var ciudades = await _service.ObtenerCiudadesAsync();
        if (ciudades == null || ciudades.Count == 0) return;

        // Si querés mostrar todas, dejá esto así.
        // Si querés SOLO activas: descomentá la línea de abajo y comentá la otra.
        _ciudades = ciudades;
        // _ciudades = ciudades.Where(c => c.Activa).ToList();

        foreach (var c in _ciudades)
            LocalidadPicker.Items.Add(c.Nombre);

        int ciudadIdGuardada = Preferences.Get("ciudad_id", _ciudades[0].Id);
        int idx = _ciudades.FindIndex(c => c.Id == ciudadIdGuardada);
        LocalidadPicker.SelectedIndex = idx >= 0 ? idx : 0;

        LocalidadPicker.SelectedIndexChanged -= LocalidadPicker_SelectedIndexChanged;
        LocalidadPicker.SelectedIndexChanged += LocalidadPicker_SelectedIndexChanged;
    }

    private void LocalidadPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        int i = LocalidadPicker.SelectedIndex;
        if (i < 0 || i >= _ciudades.Count) return;

        var ciudad = _ciudades[i];
        Preferences.Set("ciudad_id", ciudad.Id);
        Preferences.Set("ciudad_nombre", ciudad.Nombre);
    }
}
