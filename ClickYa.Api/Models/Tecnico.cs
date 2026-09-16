using System.Text.Json.Serialization;

public class Tecnico
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Rubro { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    [JsonIgnore]
    public string Token { get; set; } = "";
    public bool Activo { get; set; } = true;
    public int? CategoriaId { get; set; }

    // NUEVO
    public bool EsPremium { get; set; } = false;
    public string FotoPortada { get; set; } = ""; // URL o base64
    public string Logo { get; set; } = "";
    public string Ubicacion { get; set; } = ""; // lat,lng
    public string Direccion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    // Inicio de la membresía/última renovación. La vigencia termina 30 días después.
    public DateTime? FechaPremium { get; set; } = null;
    public string Instagram { get; set; } = "";
    public double Latitud { get; set; } = 0;
    public double Longitud { get; set; } = 0;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool PremiumVigente => ClickYa.Api.Models.PremiumMembership.IsVigente(this, DateTime.UtcNow);
}
