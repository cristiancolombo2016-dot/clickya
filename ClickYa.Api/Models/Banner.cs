namespace ClickYa.Api.Models
{
    public class Banner
    {
        public int Id { get; set; }
        public string Seccion { get; set; } = "home";
        public string ImagenUrl { get; set; } = "";
        public int LocalId { get; set; }
        public int TecnicoId { get; set; }
        public int Dias { get; set; } = 7;
        public DateTime Inicio { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;
    }
}