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

        // Endpoint extra para el Admin (Web)
        [HttpGet("Pendientes")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pendientes()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.EstadoPedido)
                .Include(p => p.Detalles)
                .Include(p => p.Usuario)
                // Filtramos por nombre del estado en la tabla relacionada
                .Where(p => p.EstadoPedido.Nombre != "ENTREGADO" && p.EstadoPedido.Nombre != "RECHAZADO")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            var respuestaMovil = pedidos.Select(p => new
            {
                p.IdPedido,
                p.FechaPedido,
                p.Total,
                Estado = p.EstadoPedido != null ? p.EstadoPedido.Nombre : "Desconocido",
                Usuario = p.Usuario != null ? p.Usuario.FullName : "Sin Nombre",
                p.Detalles
            });

            return Ok(respuestaMovil);
        }
    }
}