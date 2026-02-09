using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Data
{
    public class AppDbContext : IdentityDbContext<Users, Roles, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // --- TABLAS DE NEGOCIO ---
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallePedido { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<CategoriaProducto> CategoriaProducto { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<TipoPago> TiposPago { get; set; }
        public DbSet<EstadoPago> EstadosPago { get; set; }
        public DbSet<EstadoPedido> EstadoPedidos { get; set; }
        public DbSet<Bitacora> Bitacoras { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<DetallesProducto> DetallesProductos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetalleFacturas { get; set; }


        // El error suele estar aquí: asegúrate de que sea "protected override void"
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Siempre al principio

            // 1. Mapeo de Tablas Identity
            builder.Entity<Users>().ToTable("Usuarios");
            builder.Entity<Roles>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UsuarioRoles");

            builder.Entity<Users>(b =>
            {
                b.Property(u => u.Id).HasColumnName("IdUsuario");
                b.Property(u => u.FullName).HasColumnName("Nombre");
            });

            builder.Entity<Roles>(b => b.Property(r => r.Id).HasColumnName("IdRol"));

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal));

                foreach (var property in properties)
                {
                    property.SetPrecision(10);
                    property.SetScale(2);
                }
            }

            // 3. TriggersLegacy
            builder.Entity<DetallesProducto>().ToTable("detalles_producto", table =>
            {
                table.HasTrigger("TR_ActualizarStock_Producto");
            });

            builder.Entity<Factura>(entity =>
            {
                entity.ToTable("Facturas");
                entity.HasOne(f => f.Pedido)
                      .WithOne()
                      .HasForeignKey<Factura>(f => f.IdPedido)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                entity.HasOne(f => f.TipoPago)
                      .WithMany()
                      .HasForeignKey(f => f.IdTipoPago);
            });

            builder.Entity<DetalleFactura>(entity =>
            {
                entity.HasOne(df => df.Factura)
                      .WithMany(f => f.Detalles)
                      .HasForeignKey(df => df.IdFactura)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<IdentityUserRole<int>>().HasKey(p => new { p.UserId, p.RoleId });
        }
    }
}