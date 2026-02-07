using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
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

        public CatalogoModel(AppDbContext context, UserManager<Users> userManager, Services.IAuditoriaService auditoria)
        {
            _context = context;
            _userManager = userManager;
            _auditoria = auditoria;
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

        // -------------------------------------------------------------------------
        // CORRECCIÓN AQUÍ: Agregamos 'idTipoPago' y 'referencia' a los parámetros
        // -------------------------------------------------------------------------
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

                var factura = new Factura
                {
                    IdTipoPago = idTipoPago,
                    FechaEmision = DateTime.Now,
                    RucCedula = string.IsNullOrEmpty(rucCliente) ? "9999999999999" : rucCliente,
                    NombreCliente = string.IsNullOrEmpty(nombreCliente) ? user.UserName : nombreCliente,
                    Estado = "PAGADA",
                    Detalles = new List<DetalleFactura>()
                };

                decimal subtotalAcumulado = 0;

                // 2. Procesar Productos (Calculamos el Subtotal)
                foreach (var item in carrito)
                {
                    var productoDb = await _context.Productos.FindAsync(item.Id);

                    if (productoDb == null) throw new Exception($"El producto ID {item.Id} ya no existe.");
                    if (productoDb.Cantidad < item.Cantidad) throw new Exception($"Stock insuficiente para {productoDb.NombreProducto}");

                    decimal subtotalLinea = productoDb.Precio * item.Cantidad;
                    subtotalAcumulado += subtotalLinea;

                    // Llenar detalles...
                    pedido.Detalles.Add(new DetallePedido
                    {
                        IdProducto = item.Id,
                        NombreProductoSnapshot = productoDb.NombreProducto,
                        PrecioUnitarioSnapshot = productoDb.Precio,
                        Cantidad = item.Cantidad,
                        Subtotal = subtotalLinea
                    });

                    factura.Detalles.Add(new DetalleFactura
                    {
                        ProductoNombre = productoDb.NombreProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = productoDb.Precio,
                        SubtotalLinea = subtotalLinea
                    });

                    _context.DetallesProductos.Add(new DetallesProducto
                    {
                        IdProducto = item.Id,
                        Cantidad = item.Cantidad,
                        TipoEstado = "pedido",
                        Precio = productoDb.Precio,
                        Fecha = DateTime.Now,
                        IdTransaccion = 0
                    });
                }

                // 3. Cálculos Finales
                pedido.Total = subtotalAcumulado;
                factura.Subtotal = subtotalAcumulado;
                factura.Iva = subtotalAcumulado * 0.15m;
                factura.Total = factura.Subtotal + factura.Iva;

                // --- 4. VALIDACIÓN DE PAGO EN EFECTIVO (AQUÍ ESTÁ LA MAGIA) ---
                string referenciaFormateada = referencia;

                if (idTipoPago == 2) // EFECTIVO
                {
                    // Intentamos convertir lo que escribió el usuario a número
                    if (decimal.TryParse(referencia, out decimal montoEntregado))
                    {
                        // VALIDACIÓN: ¿Alcanza el dinero?
                        // Usamos Math.Round para evitar problemas de decimales (ej: 19.99999 vs 20)
                        if (montoEntregado < Math.Round(factura.Total, 2))
                        {
                            throw new Exception($"El monto ingresado (${montoEntregado}) es insuficiente. El total es ${factura.Total:F2}");
                        }

                        // Si alcanza, calculamos el cambio
                        decimal cambio = montoEntregado - factura.Total;
                        referenciaFormateada = $"Paga con: ${montoEntregado:F2} - Cambio: ${cambio:F2}";
                    }
                    else
                    {
                        throw new Exception("Por favor ingrese un monto válido para el pago en efectivo.");
                    }
                }
                else if (idTipoPago == 3) // TRANSFERENCIA
                {
                    referenciaFormateada = $"Comprobante: {referencia}";
                }
                else // TARJETA
                {
                    referenciaFormateada = "Pago con Tarjeta";
                }

                // Asignamos la referencia ya validada y formateada
                factura.ReferenciaPago = referenciaFormateada;


                // 5. Guardado en Base de Datos
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                factura.IdPedido = pedido.IdPedido;
                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarEvento("Nuevo Pedido", "Factura", factura.IdFactura,
                    $"Factura #{factura.IdFactura} ($ {factura.Total}) - Método: {idTipoPago}");

                await transaction.CommitAsync();

                TempData["LimpiarCarrito"] = true; // Señal para borrar el LocalStorage

                TempData["SuccessMessage"] = $"¡Pedido pagado! Factura #{factura.IdFactura} generada.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = $"⛔ {errorReal}"; // Quitamos texto extra para que se vea limpio
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