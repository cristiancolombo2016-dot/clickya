namespace ClickYa.Api.Models
{
    public class MensajeAdmin
    {
        public int Id { get; set; }
        public string Texto { get; set; } = "";
        public string Destino { get; set; } = "todos"; // "todos" o "tecnico" o "comercio"
        public int DestinoId { get; set; } = 0;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;
    }
}