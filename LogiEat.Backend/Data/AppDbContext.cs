using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Data
{
    // Heredamos de IdentityDbContext especificando <Usuario, Rol, TipoDeLlave>
    // Esto es vital porque en el Paso 1 decidimos usar 'int'.
    public class AppDbContext : IdentityDbContext<Users, Roles, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // --- TABLAS DE NEGOCIO (Basadas en tu script SQL) ---
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<CategoriaProducto> CategoriasProducto { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<TipoPago> TiposPago { get; set; }
        public DbSet<EstadoPago> EstadosPago { get; set; }
        public DbSet<EstadoPedido> EstadosPedido { get; set; }
        public DbSet<Bitacora> Bitacoras { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<DetallesProducto> DetallesProductos { get; set; } // Movimientos de stock

        public DbSet<CategoriaProducto> CategoriaProducto { get; set; } 
        public DbSet<DetallePedido> DetallePedido { get; set; }        

        // Este método es donde configuramos las reglas especiales de mapeo
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 1. LLAMADA BASE (OBLIGATORIA)
            // Configura internamente las tablas de Identity (Seguridad). 
            // Debe ir al principio.
            base.OnModelCreating(builder);

            // --- 1. AJUSTAR IDENTITY A TU SQL ---

            // Mapeo de Tablas
            builder.Entity<Users>().ToTable("Usuarios");
            builder.Entity<Roles>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UsuarioRoles");

            // === AGREGA ESTO: MAPEO DE COLUMNAS ===
            builder.Entity<Users>(b =>
            {
                // Le decimos que la propiedad 'Id' de C# es la columna 'IdUsuario' de SQL
                b.Property(u => u.Id).HasColumnName("IdUsuario");

                // Le decimos que la propiedad 'FullName' es la columna 'Nombre'
                b.Property(u => u.FullName).HasColumnName("Nombre");
            });

            builder.Entity<Roles>(b =>
            {
                // Lo mismo para Roles
                b.Property(r => r.Id).HasColumnName("IdRol");
            });
            // Si no pones esto, SQL Server usará decimal(18,0) y borrará los centavos.
            builder.Entity<Pedido>().Property(p => p.Total).HasPrecision(10, 2);
            builder.Entity<DetallePedido>().Property(d => d.PrecioUnitarioSnapshot).HasPrecision(10, 2);
            builder.Entity<DetallePedido>().Property(d => d.Subtotal).HasPrecision(10, 2);
            builder.Entity<Producto>().Property(p => p.Precio).HasPrecision(10, 2);
            builder.Entity<Pago>().Property(p => p.Monto).HasPrecision(10, 2);
            builder.Entity<DetallesProducto>().Property(dp => dp.Precio).HasPrecision(10, 2);

            // 4. CONFIGURACIÓN DE TRIGGERS (CRÍTICO)
            // Como tu tabla 'detalles_producto' tiene un Trigger para el stock,
            // debemos avisarle a Entity Framework. Si no lo hacemos, las inserciones fallarán.
            builder.Entity<DetallesProducto>().ToTable("detalles_producto", table =>
            {
                table.HasTrigger("TR_ActualizarStock_Producto");
            });

            // 5. LLAVES PRIMARIAS COMPUESTAS
            // La tabla UsuarioRoles en tu SQL no tiene un ID único, usa la combinación de ambos.
            builder.Entity<IdentityUserRole<int>>()
                .HasKey(p => new { p.UserId, p.RoleId });
        }
    }
}