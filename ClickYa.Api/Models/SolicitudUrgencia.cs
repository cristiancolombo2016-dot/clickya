namespace ClickYa.Api.Models
{
    public class SolicitudUrgencia
    {
        public int Id { get; set; }
        public int? TecnicoId { get; set; }
        public int? CategoriaId { get; set; }
        public string Rubro { get; set; } = "";
        public string Descripcion { get; set; } = "";
        // Campo histórico: las urgencias nuevas guardan el contacto cifrado.
        public string WhatsAppCliente { get; set; } = "";
        public string ZonaBarrio { get; set; } = "";
        public string DireccionExactaProtegida { get; set; } = "";
        public string ContactoClienteProtegido { get; set; } = "";
        public string TokenClienteHash { get; set; } = "";
        public string? FotoUrl { get; set; }
        public string Estado { get; set; } = UrgenciaEstados.Publicada;
        public DateTime Fecha { get; set; }
        public int? OfertaSeleccionadaId { get; set; }
        public DateTime? FechaSeleccion { get; set; }
    }
}
