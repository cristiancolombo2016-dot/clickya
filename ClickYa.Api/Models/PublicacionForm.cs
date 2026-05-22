using Microsoft.AspNetCore.Http;
namespace ClickYa.Api.Models
{
    public class PublicacionForm
    {
        public List<IFormFile>? Imagenes { get; set; }
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Precio { get; set; } = "";
        public string Rubro { get; set; } = "";
        public int ComercioId { get; set; }
        public string DatosExtraJson { get; set; } = "";
    }
}