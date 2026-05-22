namespace ClickYa.Api.Models
{
    public class SolicitudServicio
    {
        public int Id { get; set; }
        public string Rubro { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public string WhatsAppCliente { get; set; } = "";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Estado { get; set; } = "Pendiente";
    }
}