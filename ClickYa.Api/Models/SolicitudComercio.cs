namespace ClickYa.Api.Models
{
    public class SolicitudComercio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Rubro { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Estado { get; set; } = "NUEVO";
        public DateTime CreatedAt { get; set; }
        public string? LogoUrl { get; set; }
        public string? PortadaUrl { get; set; }
        public string? Token { get; set; }
        public int ComercioId { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EsDestacado { get; set; } = false;
    }
}