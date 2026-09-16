namespace ClickYa.Api.Models;

public class CalificacionServicio
{
    public int Id { get; set; }
    public int UrgenciaId { get; set; }
    public int TecnicoId { get; set; }
    public int Estrellas { get; set; }
    public string? Comentario { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
