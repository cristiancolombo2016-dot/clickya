using System;

namespace ClickYa.Models
{
    public class Local
    {
        public int id { get; set; }

        // Necesario para filtrar por ciudad desde Home / Locales
        public int ciudadId { get; set; }

        public string nombre { get; set; } = "";
        public string rubro { get; set; } = "";
        public string categoria { get; set; } = "";
        public string descripcion { get; set; } = "";

        public string whatsApp { get; set; } = "";
        public string instagram { get; set; } = "";
        public string ubicacion { get; set; } = "";
        public string correo { get; set; } = "";
        public string horarios { get; set; } = "";

        public string portadaUrl { get; set; } = "";
        public string portadaTipo { get; set; } = "";
        public string logoUrl { get; set; } = "";

        public string estado { get; set; } = "";
        public double latitud { get; set; } = 0;
        public double longitud { get; set; } = 0;
        public string distancia { get; set; } = "";
    }
}