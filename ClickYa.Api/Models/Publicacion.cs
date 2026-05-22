namespace ClickYa.Api.Models
{
    public class Publicacion
    {
        public int Id { get; set; }
        public int TecnicoId { get; set; }
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public List<string> Imagenes { get; set; } = new();
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}