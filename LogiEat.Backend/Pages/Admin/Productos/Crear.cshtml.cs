using LogiEat.Backend.Data;
using LogiEat.Backend.DTOs;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin.Productos
{
    [Authorize(Roles = "Admin")]
    public class CrearModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly Services.IAuditoriaService _auditoria; // <--- 1. Agregar variable
        public CrearModel(AppDbContext context, Services.IAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        [BindProperty]
        public ProductoCrearDto Input { get; set; } = new();

        public SelectList CategoriasList { get; set; }

        public async Task OnGetAsync()
        {
            // Cargamos las categorías para el dropdown
            var categorias = await _context.CategoriaProducto.ToListAsync();
            CategoriasList = new SelectList(categorias, "IdCategoria", "Nombre");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Si falla, recargamos la lista o el dropdown estará vacío
                var categorias = await _context.CategoriaProducto.ToListAsync();
                CategoriasList = new SelectList(categorias, "IdCategoria", "Nombre");
                return Page();
            }

            // Mapeo manual (DTO -> Entidad)
            var nuevoProducto = new Producto
            {
                NombreProducto = Input.Nombre,
                Precio = Input.Precio,
                Cantidad = Input.StockInicial,
                CategoriaProductoId = Input.IdCategoria
            };

            _context.Productos.Add(nuevoProducto);

            // Opcional: Registrar en Historial/Kardex (si quieres ser muy pro)
            _context.DetallesProductos.Add(new DetallesProducto
            {
                Producto = nuevoProducto,
                Cantidad = Input.StockInicial,
                TipoEstado = "inicial",
                Precio = Input.Precio,
                Fecha = DateTime.Now,
                IdTransaccion = 0
            });
            // <--- ¡SOLO ESTA LÍNEA! --->
            await _auditoria.RegistrarEvento("Crear", "Producto", nuevoProducto.IdProducto, $"Se creó el producto: {nuevoProducto.NombreProducto}");
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}