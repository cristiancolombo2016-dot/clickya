namespace ClickYa.Api.Models
{
    public class PublicacionComercio
    {
        public int Id { get; set; }
        public int ComercioId { get; set; }
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Precio { get; set; } = "";
        public string Rubro { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public List<string> ImagenesUrls { get; set; } = new();
        public string DatosExtraJson { get; set; } = "{}";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}