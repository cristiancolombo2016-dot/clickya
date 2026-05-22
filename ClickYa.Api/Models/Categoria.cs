namespace ClickYa.Api.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Seccion { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string IconoUrl { get; set; } = "";
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}