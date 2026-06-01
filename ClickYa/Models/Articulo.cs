namespace ClickYa.Models;
public class Articulo
{
    public string nombre { get; set; } = "";
    public int precio { get; set; }
    public string imagen { get; set; } = "";
    public List<string> imagenes { get; set; } = new();
    public string datosExtraJson { get; set; } = "";
    public string seccion { get; set; } = "";
    public int cantidad { get; set; } = 1;
    public int Subtotal => precio * cantidad;
    public bool TieneImagen => !string.IsNullOrWhiteSpace(imagen);
}