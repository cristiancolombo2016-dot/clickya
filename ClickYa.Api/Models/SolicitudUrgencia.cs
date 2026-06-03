namespace ClickYa.Api.Models
{
    public class SolicitudUrgencia
    {
        public int Id { get; set; }
        public int TecnicoId { get; set; }
        public string Rubro { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string WhatsAppCliente { get; set; } = "";
        public string? FotoUrl { get; set; }
        public string Estado { get; set; } = "Nueva";
        public DateTime Fecha { get; set; }
    }
}