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

            // SEGURIDAD: Solo permitimos ver la factura si pertenece al usuario logueado
            // O si el usuario es Admin (para auditoría)
            bool esAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var facturaDb = await _context.Facturas
                .Include(f => f.Detalles)
                .Include(f => f.Pedido)
                .FirstOrDefaultAsync(f => f.IdFactura == id);

            if (facturaDb == null) return NotFound();

            // Validación de dueño
            if (!esAdmin && facturaDb.Pedido.UsuarioId != user.Id)
            {
                return Forbid(); // 403 Prohibido
            }

            Factura = facturaDb;
            return Page();
        }
    }
}