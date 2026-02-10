using LogiEat.Backend.Data;
using LogiEat.Backend.DTOs;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace LogiEat.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PedidosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PedidosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> CrearPedido([FromBody] CrearPedidoDto dto)
        {
            var idUsuarioStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioStr) || !int.TryParse(idUsuarioStr, out int idUsuario))
                return Unauthorized("Token inválido.");

            if (dto.Productos == null || !dto.Productos.Any())
                return BadRequest("El carrito está vacío.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validar Stock
                foreach (var item in dto.Productos)
                {
                    var productoDb = await _context.Productos.FindAsync(item.IdProducto);
                    if (productoDb == null) throw new Exception($"Producto {item.IdProducto} no existe.");
                    if (productoDb.Cantidad < item.Cantidad) throw new Exception($"Stock insuficiente para {productoDb.NombreProducto}.");
                }

                // 2. Crear Pedido (CORRECCIÓN AQUÍ)
                // Eliminamos la línea 'Estado = "PENDIENTE"' que daba error.
                // Usamos solo IdEstadoPedido. Asegúrate de que el ID 1 exista en tu tabla EstadoPedidos.
                var nuevoPedido = new Pedido
                {
                    UsuarioId = idUsuario,
                    FechaPedido = DateTime.Now,
                    IdEstadoPedido = 1, // 1 = PENDIENTE (Debe existir en SQL)
                    Total = 0
                };

                decimal totalCalculado = 0;

                // 3. Detalles y Movimientos
                foreach (var item in dto.Productos)
                {
                    var subtotal = item.Precio * item.Cantidad;
                    totalCalculado += subtotal;

                    nuevoPedido.Detalles.Add(new DetallePedido
                    {
                        IdProducto = item.IdProducto,
                        NombreProductoSnapshot = item.Nombre,
                        PrecioUnitarioSnapshot = item.Precio,
                        Cantidad = item.Cantidad,
                        Subtotal = subtotal
                    });

                    // Trigger de SQL se encargará de restar
                    _context.DetallesProductos.Add(new DetallesProducto
                    {
                        IdProducto = item.IdProducto,
                        Cantidad = item.Cantidad,
                        TipoEstado = "pedido",
                        Precio = item.Precio,
                        Fecha = DateTime.Now,
                        IdTransaccion = 0
                    });
                }

                nuevoPedido.Total = totalCalculado;
                _context.Pedidos.Add(nuevoPedido);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Pedido creado", idPedido = nuevoPedido.IdPedido });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("MisPedidos")]
        public async Task<IActionResult> MisPedidos()
        {
            var idUsuarioStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idUsuarioStr, out int idUsuario)) return Unauthorized();

            var pedidos = await _context.Pedidos
                .Where(p => p.UsuarioId == idUsuario)
                .Include(p => p.Detalles)
                .Include(p => p.EstadoPedido) // ¡Importante! Incluir la tabla relacionada
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            // CORRECCIÓN PARA EL MÓVIL:
            // Tu App Móvil espera un campo "estado" con texto (ej: "PENDIENTE").
            // Como la tabla 'Pedidos' ya no tiene ese campo texto, lo mapeamos manualmente
            // tomando el nombre desde la tabla relacionada 'EstadoPedido'.
            var respuestaMovil = pedidos.Select(p => new
            {
                p.IdPedido,
                p.FechaPedido,
                p.Total,
                // Aquí hacemos la magia: Si hay relación, manda el nombre. Si no, "Desconocido".
                Estado = p.EstadoPedido != null ? p.EstadoPedido.Nombre : "Desconocido",
                p.Detalles
            });

            return Ok(respuestaMovil);
        }

        // Endpoint extra para el Admin (Móvil y Web)
        [HttpGet("Pendientes")]
        // 🔴 CRÍTICO: Agregamos el esquema JWT y aseguramos el Rol
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> Pendientes()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.EstadoPedido)
                .Include(p => p.Detalles)
                .Include(p => p.Usuario)
                // Filtramos los que aún no están terminados
                .Where(p => p.EstadoPedido.Nombre != "ENTREGADO" && p.EstadoPedido.Nombre != "RECHAZADO")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            // Mapeamos al mismo formato que espera el móvil para no tener errores de lectura
            var respuesta = pedidos.Select(p => new
            {
                p.IdPedido,
                p.FechaPedido,
                p.Total,
                // Usamos el nombre del estado para la lógica de colores del móvil
                Estado = p.EstadoPedido != null ? p.EstadoPedido.Nombre : "PENDIENTE",
                Detalles = p.Detalles.Select(d => new {
                    d.NombreProductoSnapshot,
                    d.Cantidad,
                    d.Subtotal
                })
            });

            return Ok(respuesta);
        }

        // -------------------------------------------------------
        // ZONA ADMIN (NUEVOS ENDPOINTS PARA EL MÓVIL)
        // -------------------------------------------------------

        // POST: api/Pedidos/Aprobar/5
        // Este endpoint replica la lógica de la Web: Aprueba y Genera Factura
        [HttpPost("Aprobar/{id}")]
        [Authorize(Roles = "Admin,Cocina")] // Ajusta los roles según necesites
        public async Task<IActionResult> AprobarPedido(int id, [FromServices] LogiEat.Backend.Services.Facturacion.IFacturacionService facturacionService)
        {
            try
            {
                // Reutilizamos tu servicio de facturación que ya contiene la lógica de negocio y transacción
                var factura = await facturacionService.GenerarFacturaPorAprobacionAsync(id);
                return Ok(new { mensaje = "Pedido aprobado y facturado", idFactura = factura.IdFactura });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: api/Pedidos/CambiarEstado/5?nuevoEstado=RECHAZADO
        [HttpPost("CambiarEstado/{id}")]
        [Authorize(Roles = "Admin,Cocina")]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound("Pedido no encontrado");

            // Buscamos el ID del estado basado en el texto (ej: "RECHAZADO")
            // Usamos Contains para ser flexibles con mayúsculas/minúsculas parciales
            var estadoDb = await _context.EstadoPedidos
                .FirstOrDefaultAsync(e => e.Nombre.Contains(nuevoEstado));

            if (estadoDb == null)
                return BadRequest($"El estado '{nuevoEstado}' no existe en la base de datos.");

            pedido.IdEstadoPedido = estadoDb.IdEstadoPedido;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Estado actualizado a {estadoDb.Nombre}" });
        }
    }
}