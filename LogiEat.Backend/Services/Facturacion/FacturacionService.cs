using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Services.Facturacion
{
    public class FacturacionService : IFacturacionService
    {
        private readonly AppDbContext _context;
        private readonly IAuditoriaService _auditoria; // Inyectamos auditoría aquí también

        public FacturacionService(AppDbContext context, IAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        public Factura CalcularFactura(Pedido pedido, List<DetallePedido> detalles, decimal descuento)
        {
            // ... (MANTEN TU CÓDIGO DE CÁLCULO AQUÍ IGUAL QUE ANTES) ...
            // Este método solo hace matemáticas, no toca la BD.
            decimal subtotal = Math.Round(detalles.Sum(d => d.Cantidad * d.PrecioUnitarioSnapshot), 2);
            decimal iva = Math.Round(subtotal * 0.15m, 2);
            // ... retornar nueva Factura ...
            return new Factura { /* ... */ };
        }

        public async Task<Factura> GenerarFacturaPorAprobacionAsync(int idPedido)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cargamos el pedido con tracking para poder actualizar su estado
                var pedido = await _context.Pedidos
                    .Include(p => p.Detalles)
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p => p.IdPedido == idPedido);

                if (pedido == null) throw new Exception("Pedido no encontrado.");

                var facturaExiste = await _context.Facturas.AnyAsync(f => f.IdPedido == idPedido);
                if (facturaExiste) throw new Exception("Este pedido ya fue facturado.");

                // 3. Cambiar estado del pedido
                var estadoCocina = await _context.EstadoPedidos
                    .FirstOrDefaultAsync(e => e.Nombre.Contains("PREPARACION") || e.Nombre.Contains("COCINA"));

                if (estadoCocina != null)
                    pedido.IdEstadoPedido = estadoCocina.IdEstadoPedido;

                // 4. Crear el objeto Factura (Cálculos manuales para asegurar precisión)
                decimal subtotal = Math.Round(pedido.Detalles.Sum(d => d.Subtotal), 2);
                decimal iva = Math.Round(subtotal * 0.15m, 2);
                decimal total = subtotal + iva;

                var factura = new Factura
                {
                    IdPedido = pedido.IdPedido, // Asignamos ID, NO el objeto completo
                    FechaEmision = DateTime.Now,
                    RucCedula = "9999999999999", // Valor por defecto requerido
                    NombreCliente = pedido.Usuario?.FullName ?? "Consumidor Final",
                    Subtotal = subtotal,
                    UsuarioId = pedido.UsuarioId,
                    Iva = iva,
                    Total = total,
                    Estado = "PAGADA",
                    IdTipoPago = 1, // ⚠️ ASEGÚRATE que el ID 1 exista en tu tabla TipoPago
                    Detalles = pedido.Detalles.Select(d => new DetalleFactura
                    {
                        ProductoNombre = d.NombreProductoSnapshot,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitarioSnapshot,
                        SubtotalLinea = d.Subtotal
                    }).ToList()
                };

                // 5. Guardar Factura
                _context.Facturas.Add(factura);

                // 6. Persistir cambios de Pedido y Factura
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarEvento("Facturación Automática", "Pedido", idPedido,
                    $"Factura #{factura.IdFactura} generada automáticamente tras aprobación.");

                await transaction.CommitAsync();
                return factura;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Logueamos el error real para que lo veas en la consola de depuración
                Console.WriteLine($"DB ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"INNER ERROR: {ex.InnerException.Message}");

                throw new Exception($"Error en DB: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<Factura> CrearFacturaDirectaAsync(int idCliente, List<DetallePedido> items, int idTipoPago, string ruc, string nombre)
        {
            // --- CORRECCIÓN PARA TEST #6: VALIDACIÓN DE ENTRADA ---
            if (items == null || !items.Any())
            {
                throw new Exception("No se puede generar una factura sin productos.");
            }
            using var transaction = await _context.Database.BeginTransactionAsync(); // RNF-04: Transaccionalidad 
            try
            {
                // 1. Cálculos de totales
                decimal subtotal = Math.Round(items.Sum(i => i.Cantidad * i.PrecioUnitarioSnapshot), 2);
                decimal iva = Math.Round(subtotal * 0.15m, 2);

                var factura = new Factura
                {
                    IdPedido = null, // Regla de Negocio 9.3 [cite: 81]
                    UsuarioId = idCliente, // RF-03: Asociación cliente [cite: 60]
                    FechaEmision = DateTime.Now,
                    RucCedula = ruc,
                    NombreCliente = nombre,
                    Subtotal = subtotal,
                    Iva = iva,
                    Total = subtotal + iva,
                    IdTipoPago = idTipoPago,
                    Estado = "PAGADA"
                };

                // 2. Procesar cada producto para reducir stock y crear detalles
                foreach (var item in items)
                {
                    var productoDb = await _context.Productos.FindAsync(item.IdProducto);

                    if (productoDb == null) throw new Exception($"El producto {item.IdProducto} no existe.");

                    // Validación de integridad: ¿hay suficiente stock?
                    if (productoDb.Cantidad < item.Cantidad)
                        throw new Exception($"Stock insuficiente para {productoDb.NombreProducto}.");

                    // REDUCIR STOCK
                    productoDb.Cantidad -= item.Cantidad;

                    // REGISTRAR MOVIMIENTO (Para el Trigger o historial de stock)
                    _context.DetallesProductos.Add(new DetallesProducto
                    {
                        IdProducto = item.IdProducto,
                        Cantidad = item.Cantidad,
                        TipoEstado = "venta_directa", // Diferenciamos de 'pedido'
                        Precio = item.PrecioUnitarioSnapshot,
                        Fecha = DateTime.Now,
                        IdTransaccion = 0
                    });

                    // Añadir al detalle de la factura
                    factura.Detalles.Add(new DetalleFactura
                    {
                        ProductoNombre = item.NombreProductoSnapshot,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitarioSnapshot,
                        SubtotalLinea = item.Cantidad * item.PrecioUnitarioSnapshot
                    });
                }

                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarEvento("Venta Directa", "Factura", factura.IdFactura,
                    $"Venta realizada por vendedor. Stock actualizado.");

                await transaction.CommitAsync();
                return factura;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}