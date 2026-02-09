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
        private readonly IAuditoriaService _auditoria;

        // SE ELIMINÓ: IFacturacionService (La factura ahora se genera en la aprobación)

        public CatalogoModel(AppDbContext context, UserManager<Users> userManager, IAuditoriaService auditoria)
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
                // 1. Configuración de Estado inicial (Esperando Aprobación)
                var estadoEsperando = await _context.EstadoPedidos
                    .FirstOrDefaultAsync(e => e.Nombre.Contains("PAGADO") || e.Nombre.Contains("ESPERANDO"));

                int idEstadoInicial = estadoEsperando?.IdEstadoPedido ?? 1;

                var pedido = new Pedido
                {
                    UsuarioId = user.Id,
                    FechaPedido = DateTime.Now,
                    IdEstadoPedido = idEstadoInicial,
                    Detalles = new List<DetallePedido>(),
                    IdTransaccionPago = referencia // Guardamos la referencia para que el Admin la verifique
                };

                decimal subtotalPedido = 0;

                // 2. Procesar Productos y Validar Stock
                foreach (var item in carrito)
                {
                    // Obtenemos el precio directamente de la BD (Seguridad: evita manipulación de precios)
                    var productoDb = await _context.Productos.FindAsync(item.Id);

                    if (productoDb == null) throw new Exception($"El producto ID {item.Id} ya no existe.");
                    if (productoDb.Cantidad < item.Cantidad) throw new Exception($"Stock insuficiente para {productoDb.NombreProducto}");

                    var subtotalLinea = productoDb.Precio * item.Cantidad;
                    subtotalPedido += subtotalLinea;

                    pedido.Detalles.Add(new DetallePedido
                    {
                        IdProducto = item.Id,
                        NombreProductoSnapshot = productoDb.NombreProducto,
                        PrecioUnitarioSnapshot = productoDb.Precio,
                        Cantidad = item.Cantidad,
                        Subtotal = subtotalLinea
                    });

                    // Descontar Stock y Registrar Movimiento
                    productoDb.Cantidad -= item.Cantidad;

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

                // 3. Cálculo de Impuestos y Total Final del Pedido
                decimal iva = Math.Round(subtotalPedido * 0.15m, 2);
                pedido.Total = subtotalPedido + iva;

                // 4. Guardar Pedido (SIN FACTURA)
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // 5. Auditoría
                await _auditoria.RegistrarEvento("Creación Pedido", "Pedido", pedido.IdPedido,
                    $"Pedido #{pedido.IdPedido} creado por cliente. Esperando aprobación del supervisor.");

                await transaction.CommitAsync();

                TempData["LimpiarCarrito"] = true;
                TempData["SuccessMessage"] = $"¡Pedido #{pedido.IdPedido} realizado con éxito! Será facturado una vez que sea aprobado.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"⛔ Error: {ex.Message}";
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