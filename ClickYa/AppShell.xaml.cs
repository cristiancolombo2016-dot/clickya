using ClickYa.Views;

namespace ClickYa
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // RUTAS DE NAVEGACIÓN (NO SON SHELLCONTENT)
            Routing.RegisterRoute("locales", typeof(LocalesPage));
            Routing.RegisterRoute("local", typeof(LocalPage));
            Routing.RegisterRoute("detalleproducto", typeof(DetalleProducto));
            Routing.RegisterRoute("carrito", typeof(CarritoPage));
            Routing.RegisterRoute("tiendas-categorias", typeof(TiendasCategoriasPage));
            Routing.RegisterRoute("tiendas-lista", typeof(TiendasListaPage));
            Routing.RegisterRoute("comida-lista", typeof(ComidaListaPage));
            Routing.RegisterRoute("producto-tienda", typeof(ProductoTiendaPage));
            Routing.RegisterRoute("tienda-local", typeof(TiendaLocalPage));
            Routing.RegisterRoute("servicios-categorias", typeof(ServiciosCategoriasPage));
            Routing.RegisterRoute("profesional", typeof(ProfesionalPage));
            Routing.RegisterRoute("bares", typeof(BaresPage));
            Routing.RegisterRoute(nameof(BarPage), typeof(BarPage));
            Routing.RegisterRoute(nameof(RegistroClientePage), typeof(RegistroClientePage));
            Routing.RegisterRoute("solicitar-servicio", typeof(SolicitarServicioPage));
            Routing.RegisterRoute("tecnicos-rubro", typeof(TecnicosRubroPage));
            Routing.RegisterRoute("perfil-tecnico", typeof(PerfilTecnicoPage));
            Routing.RegisterRoute("galeria-pub", typeof(GaleriaPublicacionPage));
            Routing.RegisterRoute("buscador", typeof(BuscadorPage));
        }
    }
}
