namespace ClickYa.Api.Models
{
    public class Heladeria
    {
        public int ComercioId { get; set; }
        public List<Sabor> Sabores { get; set; } = new();
        public decimal PrecioCuarto { get; set; }
        public decimal PrecioMedio { get; set; }
        public decimal PrecioKilo { get; set; }
    }

    public class Sabor
    {
        public string Nombre { get; set; } = "";
        public bool Disponible { get; set; } = true;
    }
}