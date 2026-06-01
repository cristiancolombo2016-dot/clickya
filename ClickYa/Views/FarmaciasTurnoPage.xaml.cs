namespace ClickYa.Views;

public partial class FarmaciasTurnoPage : ContentPage
{
    private List<FarmaciaVM> _todasFarmacias = new();
    private Location? _ubicacionUsuario;

    public class FarmaciaVM : System.ComponentModel.INotifyPropertyChanged
    {
        public string Nombre { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Distancia { get; set; } = "";
        public string Grupo { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Km { get; set; } = double.MaxValue;

        private bool _expandida;
        public bool Expandida
        {
            get => _expandida;
            set { _expandida = value; OnPropertyChanged(nameof(Expandida)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string nombre) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nombre));
    }

    private static readonly List<(string Grupo, string Nombre, string Direccion, string Telefono, double Lat, double Lng)> _datos = new()
    {
        ("A", "Farmacia Armellini",      "Av. Central 3115",          "0336-4462060", -33.339, -60.213),
        ("A", "Farmacia Cabrera",        "Av. Falcón 222",             "0336-4425906", -33.338, -60.209),
        ("A", "Farmacia Diamante",       "Belgrano y Álvarez 98",      "0336-4426161", -33.336, -60.211),
        ("A", "Farmacia Furlán",         "9 de Julio 260",             "0336-4435036", -33.337, -60.210),
        ("A", "Farmacia González Pacín", "De la Nación 314",           "0336-4422330", -33.335, -60.208),
        ("A", "Farmacia Héctor López",   "Maipú 794",                  "0336-4427802", -33.334, -60.207),

        ("B", "Farmacia Carrera",        "Almafuerte 204",             "",             -33.340, -60.212),
        ("B", "Farmacia Coccaro",        "Rivadavia 987 bis",          "0336-4450202", -33.341, -60.214),
        ("B", "Farmacia Del Pueblo",     "De la Nación 450",           "0336-4425552", -33.342, -60.215),
        ("B", "Farmacia Dotto",          "Pringles 551",               "0336-4430128", -33.343, -60.213),
        ("B", "Farmacia Melone",         "Pte. Perón 858",             "0336-4441989", -33.344, -60.211),
        ("B", "Farmacia Taljame",        "Av. Alberdi 346",            "0336-4426672", -33.345, -60.210),

        ("C", "Farmacia Amefarma SCS",   "Bartolomé Mitre 200",        "0336-4425920", -33.336, -60.209),
        ("C", "Farmacia Garetto",        "Av. Morteo y España",        "0336-4424849", -33.337, -60.208),
        ("C", "Farmacia Liliana Latorre","Ameghino 347",               "0336-4452145", -33.338, -60.207),
        ("C", "Farmacia Ponce",          "Juramento 1445",             "0336-4572034", -33.339, -60.206),
        ("C", "Farmacia Rasetto",        "Luis Viale 401",             "0336-4430050", -33.340, -60.205),
        ("C", "Farmacia San Nicolás SCS","Cochabamba 357",             "",             -33.341, -60.204),

        ("D", "Farmacia Boffa",          "Av. Savio 1142",             "0336-4451628", -33.342, -60.203),
        ("D", "Farmacia CEJ",            "Maipú 495",                  "0336-4430391", -33.343, -60.202),
        ("D", "Farmacia De los Arroyos", "De la Nación 102",           "0336-4423130", -33.344, -60.201),
        ("D", "Farmacia Lombardi",       "Av. Alberdi 548",            "0336-4436606", -33.345, -60.200),
        ("D", "Farmacia Porta",          "Av. Savio 147",              "0336-4425682", -33.346, -60.199),
        ("D", "Farmacia Romero",         "Pte. Perón 1648",            "0336-4443073", -33.347, -60.198),

        ("E", "Farmacia Almada",         "Maipú y J.B. Justo",         "0336-4425645", -33.348, -60.197),
        ("E", "Farmacia Catalán",        "Almafuerte 442",             "0336-4421595", -33.349, -60.196),
        ("E", "Farmacia De La Torre",    "Av. A. Illia 643",           "0336-4456839", -33.350, -60.195),
        ("E", "Farmacia Girardi",        "Av. Savio 1634",             "0336-4462724", -33.351, -60.194),
        ("E", "Farmacia Henrich",        "9 de Julio 63",              "0336-4434455", -33.352, -60.193),
        ("E", "Farmacia Tonon",          "Garibaldi 692",              "0336-4421059", -33.353, -60.192),

        ("F", "Farmacia Cantondebat",    "Brown 598",                  "0336-4428073", -33.354, -60.191),
        ("F", "Farmacia Cavara",         "Italia 350",                 "0336-4436204", -33.355, -60.190),
        ("F", "Farmacia Fénix",          "Garibaldi 281",              "0336-4423117", -33.356, -60.189),
        ("F", "Farmacia Frattini",       "Moreno 108",                 "0336-4452182", -33.357, -60.188),
        ("F", "Farmacia Prat",           "Pte. Perón 1093",            "0336-4441803", -33.358, -60.187),
        ("F", "Farmacia Zonta",          "Urquiza 422",                "0336-4429885", -33.359, -60.186),

        ("G", "Farmacia Barbotti",       "Bolívar 351",                "0336-4452228", -33.360, -60.185),
        ("G", "Farmacia Brasesco",       "Av. Savio 1270",             "0336-4424768", -33.361, -60.184),
        ("G", "Farmacia Cesari",         "De la Nación 183",           "0336-4427258", -33.362, -60.183),
        ("G", "Farmacia Conde",          "De la Nación 701",           "0336-4422389", -33.363, -60.182),
        ("G", "Farmacia Garaguso",       "Belgrano 320",               "0336-4429349", -33.364, -60.181),
        ("G", "Farmacia Prina",          "Av. Arturo Illia 739",       "0336-4453776", -33.365, -60.180),

        ("H", "Farmacia Blanco",         "Almafuerte y Benítez",       "0336-4420843", -33.366, -60.179),
        ("H", "Farmacia Correa",         "Italia 38",                  "0336-4428203", -33.367, -60.178),
        ("H", "Farmacia Donatelli",      "Urquiza 499",                "0336-4430017", -33.368, -60.177),
        ("H", "Farmacia Gagliardo",      "Pte. Perón 1035",            "0336-4441786", -33.369, -60.176),
        ("H", "Farmacia Salvador",       "Av. Moreno 220",             "0336-4435180", -33.370, -60.175),
        ("H", "Farmacia Tioni",          "Rademil y Alvear",           "0336-4434809", -33.371, -60.174),
        ("H", "Farmacia Farias",         "Av. Savio 238",              "0336-4437501", -33.372, -60.173),

        ("I", "Farmacia Alonso",         "Don Bosco y Pellegrini",     "0336-4437687", -33.373, -60.172),
        ("I", "Farmacia Ciminari",       "Alberdi 699",                "0336-4421664", -33.374, -60.171),
        ("I", "Farmacia García",         "Belgrano 184",               "0336-4422324", -33.375, -60.170),
        ("I", "Farmacia Gómez",          "Pte. Perón 1366",            "0336-4440633", -33.376, -60.169),
        ("I", "Farmacia Leone",          "Bolívar 1053",               "0336-4431766", -33.377, -60.168),
        ("I", "Farmacia Plaza Sarmiento","España y Rivadavia",         "0336-4424231", -33.378, -60.167),

        ("J", "Farmacia Alluchón",       "Olleros 55",                 "0336-4430164", -33.379, -60.166),
        ("J", "Farmacia Bongiorno",      "Francia y Alberdi",          "0336-4424936", -33.380, -60.165),
        ("J", "Farmacia Bracco",         "Av. Savio 373",              "0336-4424389", -33.381, -60.164),
        ("J", "Farmacia Floreani",       "Garibaldi y Alem",           "0336-4424303", -33.382, -60.163),
        ("J", "Farmacia Pinasco",        "Alvear 95",                  "0336-4421261", -33.383, -60.162),
        ("J", "Farmacia Prado",          "M. Cernadas 110",            "0336-4439209", -33.384, -60.161),

        ("K", "Farmacia Andrada",        "Av. Savio 601",              "0336-4452855", -33.385, -60.160),
        ("K", "Farmacia María I. López", "L. Guruciaga 103",           "0336-4420142", -33.386, -60.159),
        ("K", "Farmacia Martinelli",     "Av. Pte. Illía 1127",        "0336-4422779", -33.387, -60.158),
        ("K", "Farmacia Palau",          "Av. Central 2215",           "0336-4462030", -33.388, -60.157),
        ("K", "Farmacia Radium",         "De la Nación 352",           "0336-4422327", -33.389, -60.156),
        ("K", "Farmacia Tonello",        "Av. Falcón 651",             "0336-4429882", -33.390, -60.155),

        ("L", "Farmacia Baroni",         "Av. Irigoyen 1272",          "0336-4463087", -33.391, -60.154),
        ("L", "Farmacia Capra",          "Moreno 466",                 "0336-4433606", -33.392, -60.153),
        ("L", "Farmacia Hegouaburu",     "Bartolomé Mitre 601",        "0336-4426551", -33.393, -60.152),
        ("L", "Farmacia Maccaroni",      "Av. Savio 725",              "0336-4436762", -33.394, -60.151),
        ("L", "Farmacia Menna",          "Rivadavia 501",              "0336-4437078", -33.395, -60.150),
        ("L", "Farmacia Pucciarelli",    "Lavalle 215 bis",            "0336-4422016", -33.396, -60.149),
    };

    public static string ObtenerGrupoHoy()
    {
        var referencia = new DateTime(2026, 5, 31);
        var hoy = DateTime.Now.Hour >= 8 ? DateTime.Today : DateTime.Today.AddDays(-1);
        int diasDesdeRef = (int)(hoy - referencia).TotalDays;
        int indice = ((5 + diasDesdeRef) % 12 + 12) % 12;
        return ((char)('A' + indice)).ToString();
    }

    public FarmaciasTurnoPage()
    {
        InitializeComponent();
        _ = IniciarAsync();
    }

    private async Task IniciarAsync()
    {
        var grupoHoy = ObtenerGrupoHoy();

        try
        {
            var ubicacion = await Geolocation.GetLastKnownLocationAsync()
                ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low));
            _ubicacionUsuario = ubicacion;
        }
        catch { }

        _todasFarmacias = _datos.Select(d =>
        {
            double km = double.MaxValue;
            if (_ubicacionUsuario != null)
                km = Location.CalculateDistance(_ubicacionUsuario.Latitude, _ubicacionUsuario.Longitude,
                    d.Lat, d.Lng, DistanceUnits.Kilometers);

            return new FarmaciaVM
            {
                Nombre = d.Nombre,
                Grupo = d.Grupo,
                Direccion = d.Direccion,
                Telefono = d.Telefono,
                Estado = d.Grupo == grupoHoy ? "🟢 En turno" : "⚪ Fuera de turno",
                Km = km,
                Distancia = km == double.MaxValue ? "" :
                    (km < 1 ? $"📍 {(int)(km * 1000)} m" : $"📍 {km:F1} km")
            };
        })
        .OrderBy(f => f.Estado != "🟢 En turno")
        .ThenBy(f => f.Km)
        .ToList();

        MainThread.BeginInvokeOnMainThread(() => MostrarEnTurno());
    }

    private void MostrarEnTurno()
    {
        foreach (var f in _todasFarmacias) f.Expandida = false;
        ListaFarmacias.ItemsSource = null;
        ListaFarmacias.ItemsSource = _todasFarmacias
            .Where(f => f.Estado == "🟢 En turno")
            .ToList();
    }

    private void MostrarTodas()
    {
        foreach (var f in _todasFarmacias) f.Expandida = false;
        ListaFarmacias.ItemsSource = null;
        ListaFarmacias.ItemsSource = _todasFarmacias;
    }

    private async void Volver_Tapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void Ubicacion_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            var ubicacion = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium));
            _ubicacionUsuario = ubicacion;
            await IniciarAsync();
        }
        catch
        {
            await DisplayAlert("Ubicación", "No se pudo obtener tu ubicación", "OK");
        }
    }

    private void Farmacia_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FarmaciaVM farmacia) return;

        foreach (var f in _todasFarmacias)
            if (f != farmacia) f.Expandida = false;

        farmacia.Expandida = !farmacia.Expandida;
    }

    private async void OpcionMaps_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FarmaciaVM f) return;
        var url = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(f.Nombre + " " + f.Direccion + " San Nicolás")}";
        await Launcher.OpenAsync(url);
    }

    private async void OpcionLlamar_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FarmaciaVM f) return;
        await Launcher.OpenAsync($"tel:{f.Telefono.Replace("-", "").Replace(" ", "")}");
    }

    private async void OpcionWhatsApp_Tapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert("WhatsApp no disponible",
            "Esta farmacia no tiene WhatsApp registrado. Podés llamarla directamente.",
            "OK");
    }
    private void FiltroEnTurno_Tapped(object sender, TappedEventArgs e)
    {
        BtnEnTurno.BackgroundColor = Color.FromArgb("#079B2E");
        BtnTodas.BackgroundColor = Color.FromArgb("#121A26");
        MostrarEnTurno();
    }

    private void FiltroTodas_Tapped(object sender, TappedEventArgs e)
    {
        BtnTodas.BackgroundColor = Color.FromArgb("#079B2E");
        BtnEnTurno.BackgroundColor = Color.FromArgb("#121A26");
        MostrarTodas();
    }
}