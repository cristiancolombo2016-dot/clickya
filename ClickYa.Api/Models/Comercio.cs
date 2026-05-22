namespace ClickYa.Api.Models
{
    public class Comercio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Rubro { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string WhatsApp { get; set; } = string.Empty;
        public string Instagram { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Horarios { get; set; } = string.Empty;
        public string PortadaUrl { get; set; } = string.Empty;
        public string PortadaTipo { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string Estado { get; set; } = "Activo";
        public string Token { get; set; } = string.Empty;
        public double Latitud { get; set; } = 0;
        public double Longitud { get; set; } = 0;
        public bool EsDestacado { get; set; } = false;
    }
}