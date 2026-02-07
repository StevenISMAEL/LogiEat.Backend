using LogiEat.Backend.Data;
using LogiEat.Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin.Productos
{
    [Authorize(Roles = "Admin")]
    public class EditarModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly Services.IAuditoriaService _auditoria;
        public EditarModel(AppDbContext context, Services.IAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        [BindProperty]
        public ProductoCrearDto Input { get; set; } = new();

        public SelectList CategoriasList { get; set; }

        // CARGAR DATOS
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            // Llenamos el DTO con los datos actuales de la BD
            Input = new ProductoCrearDto
            {
                Nombre = producto.NombreProducto,
                Precio = producto.Precio,
                StockInicial = producto.Cantidad, // Aquí mostramos el stock actual
                IdCategoria = producto.CategoriaProductoId ?? 0
            };

            // Cargar dropdown
            var categorias = await _context.CategoriaProducto.ToListAsync();
            CategoriasList = new SelectList(categorias, "IdCategoria", "Nombre", producto.CategoriaProductoId);

            return Page();
        }

        // GUARDAR CAMBIOS
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                var categorias = await _context.CategoriaProducto.ToListAsync();
                CategoriasList = new SelectList(categorias, "IdCategoria", "Nombre");
                return Page();
            }

            var productoDb = await _context.Productos.FindAsync(id);
            if (productoDb == null) return NotFound();

            // Actualizamos los campos
            productoDb.NombreProducto = Input.Nombre;
            productoDb.Precio = Input.Precio;
            productoDb.CategoriaProductoId = Input.IdCategoria;

            // Opcional: Permitir editar stock directamente desde aquí
            // Si prefieres que el stock solo se mueva por compras/ventas, comenta esta línea.
            productoDb.Cantidad = Input.StockInicial;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id)) return NotFound();
                else throw;
            }
            // 3. LA LÍNEA MÁGICA DE AUDITORÍA
            await _auditoria.RegistrarEvento("Editar", "Producto", id, $"Se actualizaron los datos de: {productoDb.NombreProducto}");
            return RedirectToPage("./Index");
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.IdProducto == id);
        }
    }
}