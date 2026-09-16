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
        public DbSet<OfertaUrgencia> OfertasUrgencia { get; set; }
        public DbSet<CalificacionServicio> CalificacionesServicio { get; set; }
        public DbSet<PublicacionComercio> PublicacionesComercios { get; set; }
        public DbSet<SolicitudServicio> SolicitudesServicio { get; set; }
        public DbSet<Reporte> Reportes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tecnico>().HasOne<Categoria>().WithMany()
                .HasForeignKey(t => t.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SolicitudUrgencia>().HasOne<Categoria>().WithMany()
                .HasForeignKey(u => u.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SolicitudUrgencia>().HasOne<Tecnico>().WithMany()
                .HasForeignKey(u => u.TecnicoId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OfertaUrgencia>()
                .HasIndex(o => new { o.UrgenciaId, o.TecnicoId }).IsUnique();
            modelBuilder.Entity<OfertaUrgencia>().HasOne<SolicitudUrgencia>().WithMany()
                .HasForeignKey(o => o.UrgenciaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OfertaUrgencia>().HasOne<Tecnico>().WithMany()
                .HasForeignKey(o => o.TecnicoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OfertaUrgencia>().Property(o => o.PrecioEstimado).HasPrecision(12, 2);
            modelBuilder.Entity<OfertaUrgencia>().ToTable(t =>
                t.HasCheckConstraint("CK_OfertasUrgencia_Precio", "\"PrecioEstimado\" >= 0"));

            modelBuilder.Entity<SolicitudUrgencia>().HasOne<OfertaUrgencia>().WithMany()
                .HasForeignKey(u => u.OfertaSeleccionadaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SolicitudUrgencia>().HasIndex(u => u.OfertaSeleccionadaId).IsUnique();

            modelBuilder.Entity<CalificacionServicio>().HasIndex(c => c.UrgenciaId).IsUnique();
            modelBuilder.Entity<CalificacionServicio>().HasOne<SolicitudUrgencia>().WithOne()
                .HasForeignKey<CalificacionServicio>(c => c.UrgenciaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CalificacionServicio>().HasOne<Tecnico>().WithMany()
                .HasForeignKey(c => c.TecnicoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CalificacionServicio>().ToTable(t =>
                t.HasCheckConstraint("CK_CalificacionesServicio_Estrellas", "\"Estrellas\" BETWEEN 1 AND 5"));
        }
    }
}
