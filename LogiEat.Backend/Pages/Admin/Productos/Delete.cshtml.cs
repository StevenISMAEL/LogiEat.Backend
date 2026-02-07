using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using LogiEat.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin.Productos
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuditoriaService _auditoria;

        public DeleteModel(AppDbContext context, IAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        [BindProperty]
        public Producto Producto { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Producto = await _context.Productos.Include(p => p.Categoria).FirstOrDefaultAsync(m => m.IdProducto == id);

            if (Producto == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var productoDb = await _context.Productos.FindAsync(id);

            if (productoDb != null)
            {
                try
                {
                    string nombreLog = productoDb.NombreProducto; // Capturamos para el log

                    _context.Productos.Remove(productoDb);
                    await _context.SaveChangesAsync();

                    // --- AUDITORÍA ---
                    await _auditoria.RegistrarEvento("Eliminar", "Producto", id, $"Se eliminó el producto: {nombreLog}");

                    return RedirectToPage("./Index");
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar: este producto ya está en pedidos de clientes.";
                    Producto = await _context.Productos.FindAsync(id);
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}