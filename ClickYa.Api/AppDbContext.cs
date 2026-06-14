using ClickYa.Api.Controllers;
using ClickYa.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace ClickYa.Api
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Comercio> Comercios { get; set; }
        public DbSet<Tecnico> Tecnicos { get; set; }
        public DbSet<Publicacion> Publicaciones { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<MensajeAdmin> MensajesAdmin { get; set; }
        public DbSet<Heladeria> Heladerias { get; set; }
        public DbSet<SolicitudComercio> Solicitudes { get; set; }
        public DbSet<SolicitudUrgencia> Urgencias { get; set; }
        public DbSet<PublicacionComercio> PublicacionesComercios { get; set; }
        public DbSet<SolicitudServicio> SolicitudesServicio { get; set; }
        public DbSet<Reporte> Reportes { get; set; }
    }
}