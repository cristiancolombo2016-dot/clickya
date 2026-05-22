using System.Collections.ObjectModel;
using System.Linq;
using ClickYa.Models;

namespace ClickYa.Services
{
    public class CarritoService
    {
        private static CarritoService _instancia;
        public static CarritoService Instancia => _instancia ??= new CarritoService();

        // 👉 Local actual del carrito
        private string? _localActual;

        public ObservableCollection<Articulo> Items { get; private set; } = new();

        private CarritoService() { }

        // ==========================
        // DEFINIR LOCAL ACTUAL
        // ==========================
        public void SetLocal(string localId)
        {
            // Si cambio de local → carrito nuevo
            if (_localActual != localId)
            {
                _localActual = localId;
                Items.Clear();
            }
        }

        // ==========================
        // AGREGAR ARTÍCULO
        // ==========================
        public void Agregar(Articulo articulo)
        {
            var existente = Items.FirstOrDefault(a => a.nombre == articulo.nombre);

            if (existente != null)
            {
                existente.cantidad++;
            }
            else
            {
                Items.Add(new Articulo
                {
                    nombre = articulo.nombre,
                    precio = articulo.precio,
                    imagen = articulo.imagen,
                    cantidad = 1
                });
            }
        }

        // ==========================
        // QUITAR ARTÍCULO
        // ==========================
        public void Quitar(Articulo articulo)
        {
            if (Items.Contains(articulo))
                Items.Remove(articulo);
        }

        // ==========================
        // VACIAR CARRITO
        // ==========================
        public void Vaciar()
        {
            Items.Clear();
        }

        // ==========================
        // TOTAL
        // ==========================
        public int Total => Items.Sum(i => i.precio * i.cantidad);
    }
}
