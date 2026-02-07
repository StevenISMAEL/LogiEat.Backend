using LogiEat.Backend.Data;
using LogiEat.Backend.DTOs;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 🔓 ZONA PÚBLICA / APP
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetProductos()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Select(p => new ProductoDto
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.NombreProducto,
                    Precio = p.Precio,
                    Stock = p.Cantidad,
                    IdCategoria = p.CategoriaProductoId,
                    // Solo usamos .Nombre de la categoría
                    NombreCategoria = p.Categoria != null ? p.Categoria.Nombre : "Sin Categoría"
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDto>> GetProducto(int id)
        {
            var p = await _context.Productos
                .Include(prod => prod.Categoria)
                .FirstOrDefaultAsync(prod => prod.IdProducto == id);

            if (p == null) return NotFound();

            return new ProductoDto
            {
                IdProducto = p.IdProducto,
                Nombre = p.NombreProducto,
                Precio = p.Precio,
                Stock = p.Cantidad,
                IdCategoria = p.CategoriaProductoId,
                NombreCategoria = p.Categoria?.Nombre ?? "N/A"
            };
        }

        // ==========================================
        // 🔒 ZONA ADMIN
        // ==========================================
        [HttpPost("Crear")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Producto>> CrearProducto([FromBody] ProductoCrearDto dto)
        {
            var categoria = await _context.CategoriaProducto.FindAsync(dto.IdCategoria);
            if (categoria == null) return BadRequest("La categoría no existe.");

            var nuevoProducto = new Producto
            {
                NombreProducto = dto.Nombre,
                Precio = dto.Precio,
                Cantidad = dto.StockInicial,
                CategoriaProductoId = dto.IdCategoria
            };

            _context.Productos.Add(nuevoProducto);
            await _context.SaveChangesAsync();

            return Ok(nuevoProducto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarProducto(int id, [FromBody] ProductoCrearDto dto)
        {
            var productoDb = await _context.Productos.FindAsync(id);
            if (productoDb == null) return NotFound();

            productoDb.NombreProducto = dto.Nombre;
            productoDb.Precio = dto.Precio;
            productoDb.CategoriaProductoId = dto.IdCategoria;

            // Nota: El stock normalmente no se edita aquí, sino por ajuste de inventario.
            // Pero si es una corrección rápida, puedes descomentar:
            // productoDb.Cantidad = dto.StockInicial; 

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            // Validación de integridad: No borrar si ya se vendió
            var tieneVentas = await _context.DetallePedido.AnyAsync(d => d.IdProducto == id);
            if (tieneVentas)
            {
                return BadRequest("No puedes eliminar este producto porque ya tiene ventas registradas.");
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}