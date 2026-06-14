namespace ClickYa.Api.Models
{
    public class Reporte
    {
        public int Id { get; set; }
        public int ComercioId { get; set; }
        public string NombreComercio { get; set; } = "";
        public string Motivo { get; set; } = "";
        public string Detalle { get; set; } = "";
        public string Estado { get; set; } = "PENDIENTE";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}