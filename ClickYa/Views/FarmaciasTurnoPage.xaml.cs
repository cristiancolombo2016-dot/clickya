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
        ("A", "Farmacia Armellini",      "Av. Central 3115",          "0336-4462060",  -33.387153666457685, -60.175830605581346),
        ("A", "Farmacia Cabrera",        "Av. Falcón 222",             "0336-4425906", -33.335852692964394, -60.20842323899019),
        ("A", "Farmacia Diamante",       "Belgrano y Álvarez 98",      "0336-4426161", -33.340458181496686, -60.23081156140524),
        ("A", "Farmacia Furlán",         "9 de Julio 260",             "0336-4435036", -33.32785183563388, -60.224832005583686),
        ("A", "Farmacia González Pacín", "De la Nación 314",           "0336-4422330", -33.33336754230814, -60.2196146883921),
        ("A", "Farmacia Héctor López",   "Maipú 794",                  "0336-4427802", -33.345847960637904, -60.2033680479117),

        ("B", "Farmacia Carrera",        "Almafuerte 204",             "",             -33.33524861054576, -60.213747474898796),
        ("B", "Farmacia Coccaro",        "Rivadavia 987 bis",          "0336-4450202", -33.34473955824242, -60.19379810558312),
        ("B", "Farmacia Del Pueblo",     "De la Nación 450",           "0336-4425552", -33.33599373934403, -60.22229183441901),
        ("B", "Farmacia Dotto",          "Pringles 551",               "0336-4430128", -33.34453254460829, -60.21518879024079),
        ("B", "Farmacia Melone",         "Pte. Perón 858",             "0336-4441989", -33.344195582417484, -60.23026186140504),
        ("B", "Farmacia Taljame",        "Av. Alberdi 346",            "0336-4426672", -33.329911126203086, -60.226159605583625),

        ("C", "Farmacia Amefarma SCS",   "Bartolomé Mitre 200",        "0336-4425920", -33.33022123453981, -60.21869417674798),
        ("C", "Farmacia Garetto",        "Av. Morteo y España",        "0336-4424849", -33.34514709654726, -60.22348346140494),
        ("C", "Farmacia Liliana Latorre","Ameghino 347",               "0336-4452145",-33.33548156532245, -60.2169145353489),
        ("C", "Farmacia Ponce",          "Juramento 1445",             "0336-4572034", -33.36827987031591, -60.19904941907536),
        ("C", "Farmacia Rasetto",        "Luis Viale 401",             "0336-4430050", -33.326447425222916, -60.23401866325501),
        ("C", "Farmacia San Nicolás SCS","Cochabamba 357",             "",             -33.344163961138044, -60.2057440649607),

        ("D", "Farmacia Boffa",          "Av. Savio 1142",             "0336-4451628", -33.35388502472432, -60.19668279024027),
        ("D", "Farmacia CEJ",            "Maipú 495",                  "0336-4430391", -33.34209726386566, -60.20903533256964),
        ("D", "Farmacia De los Arroyos", "De la Nación 102",           "0336-4423130", -33.329233553169196, -60.21575564791249),
        ("D", "Farmacia Lombardi",       "Av. Alberdi 548",            "0336-4436606", -33.33395716240175, -60.2301132037344),
        ("D", "Farmacia Porta",          "Av. Savio 147",              "0336-4425682", -33.33845288139246, -60.21922039024105),
        ("D", "Farmacia Romero",         "Pte. Perón 1648",            "0336-4443073", -33.34474227473956, -60.24404496388165),

        ("E", "Farmacia Almada",         "Maipú y J.B. Justo",         "0336-4425645", -33.338093570487715, -60.21494602092561),
        ("E", "Farmacia Catalán",        "Almafuerte 442",             "0336-4421595", -33.3392484727454, -60.208148061405254),
        ("E", "Farmacia De La Torre",    "Av. A. Illia 643",           "0336-4456839", -33.31733846448554, -60.23113874606395),
        ("E", "Farmacia Girardi",        "Av. Savio 1634",             "0336-4462724", -33.36035705808045, -60.187382319075596),
        ("E", "Farmacia Henrich",        "9 de Julio 63",              "0336-4434455", -33.330977900440246, -60.22005344791252),
        ("E", "Farmacia Tonon",          "Garibaldi 692",              "0336-4421059", -33.342418901806006, -60.22470230558309),

        ("F", "Farmacia Cantondebat",    "Brown 598",                  "0336-4428073", -33.35054114674089, -60.213508422774126),
        ("F", "Farmacia Cavara",         "Italia 350",                 "0336-4436204", -33.33569831160998, -60.20845089024107),
        ("F", "Farmacia Fénix",          "Garibaldi 281",              "0336-4423117", -33.334108153011854, -60.21686873256996),
        ("F", "Farmacia Frattini",       "Moreno 108",                 "0336-4452182", -33.334419663305745, -60.22510496140566),
        ("F", "Farmacia Prat",           "Pte. Perón 1093",            "0336-4441803", -33.349559292905475, -60.235966434418344),
        ("F", "Farmacia Zonta",          "Urquiza 422",                "0336-4429885", -33.3243025024327, -60.22762111907708),

        ("G", "Farmacia Barbotti",       "Bolívar 351",                "0336-4452228", -33.33869873929749, -60.211315905583334),
        ("G", "Farmacia Brasesco",       "Av. Savio 1270",             "0336-4424768", -33.35589966771541, -60.19363670373337),
        ("G", "Farmacia Cesari",         "De la Nación 183",           "0336-4427258", -33.330681727867635, -60.217318359556685),
        ("G", "Farmacia Conde",          "De la Nación 701",           "0336-4422389", -33.34098258711903, -60.227408003734034),
        ("G", "Farmacia Garaguso",       "Belgrano 320",               "0336-4429349", -33.33177773023539, -60.222159361405721),
        ("G", "Farmacia Prina",          "Av. Arturo Illia 739",       "0336-4453776", -33.31482310238515, -60.2335225902419),

        ("H", "Farmacia Blanco",         "Almafuerte y Benítez",       "0336-4420843", -33.34134579056846, -60.20470763427235),
        ("H", "Farmacia Correa",         "Italia 38",                  "0336-4428203", -33.32993933241004, -60.216154202210916),
        ("H", "Farmacia Donatelli",      "Urquiza 499",                "0336-4430017", -33.322468180933754, -60.22934849481467),
        ("H", "Farmacia Gagliardo",      "Pte. Perón 1035",            "0336-4441786", -33.34666443678441, -60.23378307901263),
        ("H", "Farmacia Salvador",       "Av. Moreno 220",             "0336-4435180", -33.33178231599125, -60.22757031151749),
        ("H", "Farmacia Tioni",          "Rademil y Alvear",           "0336-4434809", -33.356224361341084, -60.19771087288189),
        ("H", "Farmacia Farias",         "Av. Savio 238",              "0336-4437501", -33.33892695594811, -60.21730524961895),

        ("I", "Farmacia Alonso",         "Don Bosco y Pellegrini",     "0336-4437687", -33.33548149882892, -60.220308803586036),
        ("I", "Farmacia Ciminari",       "Alberdi 699",                "0336-4421664", -33.33615479620352, -60.2333821035855),
        ("I", "Farmacia García",         "Belgrano 184",               "0336-4422324", -33.3287783629529, -60.219680403590495),
        ("I", "Farmacia Gómez",          "Pte. Perón 1366",            "0336-4440633", -33.352767457759946, -60.24201270357426),
        ("I", "Farmacia Leone",          "Bolívar 1053",               "0336-4431766", -33.349141306192195, -60.19596084961227),
        ("I", "Farmacia Plaza Sarmiento","España y Rivadavia",         "0336-4424231", -33.32898475503619, -60.21184039595456),

        ("J", "Farmacia Alluchón",       "Olleros 55",                 "0336-4430164", -33.33313123152798, -60.222848534277624),
        ("J", "Farmacia Bongiorno",      "Francia y Alberdi",          "0336-4424936", -33.325237898793404, -60.22286261893781),
        ("J", "Farmacia Bracco",         "Av. Savio 373",              "0336-4424389", -33.34149383387501, -60.21391397289163),
        ("J", "Farmacia Floreani",       "Garibaldi y Alem",           "0336-4424303", -33.32921179632679, -60.212434787646174),
        ("J", "Farmacia Pinasco",        "Alvear 95",                  "0336-4421261", -33.338916055835995, -60.22254896496411),
        ("J", "Farmacia Prado",          "M. Cernadas 110",            "0336-4439209", -33.34282444901402, -60.19595134220055),

        ("K", "Farmacia Andrada",        "Av. Savio 601",              "0336-4452855", -33.34537112361306, -60.208448683345416),
        ("K", "Farmacia María I. López", "L. Guruciaga 103",           "0336-4420142", -33.32515721435295, -60.220491542212216),
        ("K", "Farmacia Martinelli",     "Av. Pte. Illía 1127",        "0336-4422779", -33.342378489169626, -60.22647000358127),
        ("K", "Farmacia Palau",          "Av. Central 2215",           "0336-4462030", -33.38080016994982, -60.177086588210706),
        ("K", "Farmacia Radium",         "De la Nación 352",           "0336-4422327", -33.33353576495328, -60.2206426342775),
        ("K", "Farmacia Tonello",        "Av. Falcón 651",             "0336-4429882", -33.34448933872839, -60.219809784714315),

        ("L", "Farmacia Baroni",         "Av. Irigoyen 1272",          "0336-4463087", -33.375168241881, -60.206316311488735),
        ("L", "Farmacia Capra",          "Moreno 466",                 "0336-4433606", -33.327655497260906, -60.233750257555684),
        ("L", "Farmacia Hegouaburu",     "Bartolomé Mitre 601",        "0336-4426551", -33.337548648087115, -60.22694834961995),
        ("L", "Farmacia Maccaroni",      "Av. Savio 725",              "0336-4436762", -33.34768268734199, -60.20566093426825),
        ("L", "Farmacia Menna",          "Rivadavia 501",              "0336-4437078", -33.33696140283879, -60.204108403585),
        ("L", "Farmacia Pucciarelli",    "Lavalle 215 bis",            "0336-4422016", -33.328237428003064, -60.22139298465488),
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

        MainThread.BeginInvokeOnMainThread(() =>
        {
            LblGrupoLetra.Text = $"Grupo {grupoHoy}";
            MostrarEnTurno();
        });
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