namespace ClickYa.Models;

public class ProductoTienda
{
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public string? Descripcion { get; set; }
    public string Rubro { get; set; } = "";
    public List<string> Imagenes { get; set; } = new();
    public List<string> Talles { get; set; } = new();
    public List<string> Colores { get; set; } = new();

    // Autos
    public string Anio { get; set; } = "";
    public string ColorAuto { get; set; } = "";

    // Servicios
    public string Zona { get; set; } = "";
    public string Horario { get; set; } = "";

    // Comidas
    public string Ingredientes { get; set; } = "";
  
    public string Seccion { get; set; } = "";  // ← agregá esta línea
    public string WhatsAppTienda { get; set; } = "";
    public string NombreTienda { get; set; } = "";

    // =========================
    // PROPIEDADES DE AYUDA (UI)
    // =========================
    public bool TieneDescripcion => !string.IsNullOrWhiteSpace(Descripcion);
    public bool TieneTalles => Talles.Count > 0;
    public bool TieneColores => Colores.Count > 0;
    public bool TieneAnio => !string.IsNullOrWhiteSpace(Anio);
    public bool TieneZona => !string.IsNullOrWhiteSpace(Zona);
    public bool TieneHorario => !string.IsNullOrWhiteSpace(Horario);
    public bool TieneIngredientes => !string.IsNullOrWhiteSpace(Ingredientes);

    public string ImagenPrincipal =>
        Imagenes.Count > 0 ? Imagenes[0] : "producto_placeholder.png";
}