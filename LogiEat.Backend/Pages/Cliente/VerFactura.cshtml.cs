using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Cliente
{
    [Authorize]
    public class VerFacturaModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public VerFacturaModel(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Factura Factura { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            bool esAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            bool esVendedor = await _userManager.IsInRoleAsync(user, "Vendedor"); 

            var facturaDb = await _context.Facturas
                .Include(f => f.Detalles)
                // No es estrictamente necesario incluir Pedido para la validación de dueño
                // pero lo dejamos por si la vista lo usa.
                .Include(f => f.Pedido)
                .FirstOrDefaultAsync(f => f.IdFactura == id);

            if (facturaDb == null) return NotFound();

            // --- VALIDACIÓN DE SEGURIDAD CORREGIDA ---
            // Usamos el UsuarioId de la Factura, que nunca es nulo para ventas directas o pedidos
            if (!esAdmin && !esVendedor && facturaDb.UsuarioId != user.Id)
            {
                return Forbid();
            }

            Factura = facturaDb;
            return Page();
        }
    }
}