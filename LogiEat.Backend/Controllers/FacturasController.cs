using LogiEat.Backend.Data;
using LogiEat.Backend.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LogiEat.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // 🔒 Seguridad JWT
    public class FacturasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FacturasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Facturas/MisFacturas
        // RF-04: Consulta de historial para la App Móvil
        [HttpGet("MisFacturas")]
        public async Task<ActionResult<IEnumerable<FacturaResumenDto>>> GetMisFacturas()
        {
            // 1. Identificar al usuario que llama a la API
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // O "sub"
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            // 2. Consultar BD (Filtrando por UsuarioId para cumplir privacidad)
            var facturas = await _context.Facturas
                .Where(f => f.UsuarioId == userId) // <--- El filtro clave
                .OrderByDescending(f => f.FechaEmision)
                .Select(f => new FacturaResumenDto
                {
                    IdFactura = f.IdFactura,
                    Fecha = f.FechaEmision.ToString("yyyy-MM-dd HH:mm"), // Formato ISO amigable
                    Cliente = f.NombreCliente,
                    Ruc = f.RucCedula,
                    Total = f.Total,
                    Estado = f.Estado,
                    // Lógica para mostrar origen
                    Origen = f.IdPedido.HasValue ? $"Pedido #{f.IdPedido}" : "Compra Directa"
                })
                .ToListAsync();

            return Ok(facturas);
        }

        // GET: api/Facturas/5
        // Detalle específico para cuando toquen una factura en la lista
        [HttpGet("{id}")]
        public async Task<ActionResult<FacturaDetalleDto>> GetFactura(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var f = await _context.Facturas
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.IdFactura == id);

            if (f == null) return NotFound();

            // Seguridad: Verificar que la factura pertenezca al usuario solicitante
            if (f.UsuarioId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // Mapeo a DTO completo
            var dto = new FacturaDetalleDto
            {
                IdFactura = f.IdFactura,
                Fecha = f.FechaEmision.ToString("yyyy-MM-dd HH:mm"),
                Cliente = f.NombreCliente,
                Ruc = f.RucCedula,
                Total = f.Total,
                Subtotal = f.Subtotal,
                Iva = f.Iva,
                Estado = f.Estado,
                Origen = f.IdPedido.HasValue ? $"Pedido #{f.IdPedido}" : "Compra Directa",
                Items = f.Detalles.Select(d => new DetalleItemDto
                {
                    Producto = d.ProductoNombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.SubtotalLinea
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost("Directa")]
        [Authorize(Roles = "Admin,Vendedor")]
        public async Task<IActionResult> CrearFacturaDirecta(
        [FromBody] CrearFacturaDirectaDto dto,
        [FromServices] LogiEat.Backend.Services.Facturacion.IFacturacionService facturacionService)
        {
            try
            {
                // YA NO USAMOS EL ID DEL TOKEN. Usamos el que seleccionó el usuario.
                // Si viene null, asumimos 0 (Consumidor Final anónimo o sin cuenta)
                int idCliente = dto.IdCliente ?? 0;

                var itemsEntidad = dto.Items.Select(i => new LogiEat.Backend.Models.DetallePedido
                {
                    IdProducto = i.IdProducto,
                    Cantidad = i.Cantidad,
                    PrecioUnitarioSnapshot = 0,
                    NombreProductoSnapshot = ""
                }).ToList();

                var factura = await facturacionService.CrearFacturaDirectaAsync(
                    idCliente, // <--- Pasamos el ID seleccionado en el ComboBox
                    itemsEntidad,
                    dto.IdTipoPago,
                    dto.Ruc,
                    dto.Nombre
                );

                return Ok(new { mensaje = "Venta registrada", idFactura = factura.IdFactura });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}