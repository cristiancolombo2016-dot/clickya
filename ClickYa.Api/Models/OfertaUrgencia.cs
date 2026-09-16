namespace ClickYa.Api.Models;

public class OfertaUrgencia
{
    public int Id { get; set; }
    public int UrgenciaId { get; set; }
    public int TecnicoId { get; set; }
    public decimal PrecioEstimado { get; set; }
    public string Disponibilidad { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
