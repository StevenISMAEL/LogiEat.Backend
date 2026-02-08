using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using LogiEat.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LogiEat.Backend.Pages.Cliente
{
    [Authorize]
    public class CatalogoModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly Services.IAuditoriaService _auditoria;

        // 1. Inyectamos el servicio de facturación
        private readonly IFacturacionService _facturacionService;

        public CatalogoModel(AppDbContext context, UserManager<Users> userManager, Services.IAuditoriaService auditoria, IFacturacionService facturacionService)
        {
            _context = context;
            _userManager = userManager;
            _auditoria = auditoria;
            _facturacionService = facturacionService;
        }

        public IList<Producto> Productos { get; set; } = new List<Producto>();

        public async Task OnGetAsync()
        {
            Productos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Cantidad > 0)
                .OrderBy(p => p.NombreProducto)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostRealizarPedidoAsync(
            string cartJson,
            string rucCliente,
            string nombreCliente,
            int idTipoPago,
            string referencia)
        {
            if (string.IsNullOrEmpty(cartJson)) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            var carrito = JsonSerializer.Deserialize<List<ItemCarrito>>(cartJson);
            if (carrito == null || !carrito.Any()) return Page();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Configuración Inicial
                var estadoPagado = await _context.EstadoPedidos
                    .FirstOrDefaultAsync(e => e.Nombre.Contains("PAGADO") || e.Nombre.Contains("ESPERANDO"));
                int idEstadoFinal = estadoPagado?.IdEstadoPedido ?? 1;

                var pedido = new Pedido
                {
                    UsuarioId = user.Id,
                    FechaPedido = DateTime.Now,
                    IdEstadoPedido = idEstadoFinal,
                    Detalles = new List<DetallePedido>()
                };

                // Lista temporal para pasar al servicio de facturación
                var detallesParaFactura = new List<DetallePedido>();

                // 2. Procesar Productos (Validación de Stock y armado de lista)
                foreach (var item in carrito)
                {
                    var productoDb = await _context.Productos.FindAsync(item.Id);

                    if (productoDb == null) throw new Exception($"El producto ID {item.Id} ya no existe.");
                    if (productoDb.Cantidad < item.Cantidad) throw new Exception($"Stock insuficiente para {productoDb.NombreProducto}");

                    // Importante: Aquí solo capturamos datos, NO calculamos totales aún.
                    // El servicio se encargará de multiplicar y sumar.
                    var detalle = new DetallePedido
                    {
                        IdProducto = item.Id,
                        NombreProductoSnapshot = productoDb.NombreProducto,
                        PrecioUnitarioSnapshot = productoDb.Precio, // Precio base
                        Cantidad = item.Cantidad
                    };

                    // Agregamos a ambas listas (la del pedido y la temporal para cálculo)
                    pedido.Detalles.Add(detalle);
                    detallesParaFactura.Add(detalle);

                    // Descontar Stock
                    productoDb.Cantidad -= item.Cantidad;

                    // Registrar movimiento de inventario
                    _context.DetallesProductos.Add(new DetallesProducto
                    {
                        IdProducto = item.Id,
                        Cantidad = item.Cantidad,
                        TipoEstado = "pedido",
                        Precio = productoDb.Precio,
                        Fecha = DateTime.Now,
                        IdTransaccion = 0 // Se actualizará al guardar
                    });
                }

                // ---------------------------------------------------------------------
                // 3. EL GRAN CAMBIAZO: Delegamos la matemática al Servicio 🧠
                // ---------------------------------------------------------------------
                // Ya no hacemos: subtotal = subtotal + ...
                // Llamamos a nuestro método probado por los 20 tests unitarios:

                var facturaGenerada = _facturacionService.CalcularFactura(pedido, detallesParaFactura, 0); // 0 descuento por ahora

                // Completamos datos de UI que el servicio no conoce
                facturaGenerada.RucCedula = string.IsNullOrEmpty(rucCliente) ? "9999999999999" : rucCliente;
                facturaGenerada.NombreCliente = string.IsNullOrEmpty(nombreCliente) ? user.UserName : nombreCliente;
                facturaGenerada.Estado = "PAGADA";
                facturaGenerada.IdTipoPago = idTipoPago;

                // Actualizamos el total del pedido con el cálculo preciso del servicio
                pedido.Total = facturaGenerada.Total;

                // --- 4. VALIDACIÓN DE PAGO (Lógica de Referencia) ---
                string referenciaFormateada = referencia;

                if (idTipoPago == 2) // EFECTIVO
                {
                    if (decimal.TryParse(referencia, out decimal montoEntregado))
                    {
                        // Usamos el total calculado por el servicio
                        if (montoEntregado < facturaGenerada.Total)
                        {
                            throw new Exception($"Monto insuficiente. Total a pagar: ${facturaGenerada.Total:F2}");
                        }
                        decimal cambio = montoEntregado - facturaGenerada.Total;
                        referenciaFormateada = $"Paga con: ${montoEntregado:F2} - Cambio: ${cambio:F2}";
                    }
                    else
                    {
                        throw new Exception("Monto en efectivo inválido.");
                    }
                }
                else if (idTipoPago == 3) referenciaFormateada = $"Comprobante: {referencia}";
                else referenciaFormateada = "Pago con Tarjeta";

                facturaGenerada.ReferenciaPago = referenciaFormateada;

                // 5. Guardado en Base de Datos
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync(); // Guardamos pedido para tener el ID

                // Asignamos el ID real del pedido a la factura
                facturaGenerada.IdPedido = pedido.IdPedido;

                // Re-creamos los detalles de factura basados en el cálculo
                // (Opcional: Podrías mover esto al servicio también en el futuro)
                facturaGenerada.Detalles = detallesParaFactura.Select(d => new DetalleFactura
                {
                    ProductoNombre = d.NombreProductoSnapshot,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitarioSnapshot,
                    SubtotalLinea = d.Cantidad * d.PrecioUnitarioSnapshot
                }).ToList();

                _context.Facturas.Add(facturaGenerada);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarEvento("Nuevo Pedido", "Factura", facturaGenerada.IdFactura,
                    $"Factura #{facturaGenerada.IdFactura} ($ {facturaGenerada.Total})");

                await transaction.CommitAsync();

                TempData["LimpiarCarrito"] = true;
                TempData["SuccessMessage"] = $"¡Pedido pagado! Factura #{facturaGenerada.IdFactura} generada.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = $"⛔ {errorReal}";
                return RedirectToPage();
            }
        }

        public class ItemCarrito
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int Cantidad { get; set; }
        }
    }
}