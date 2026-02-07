using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin
{
    // Solo permitimos entrar a Administradores
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Pedido> PedidosPendientes { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Cargamos los pedidos pendientes para mostrarlos en la tabla
            PedidosPendientes = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.EstadoPedido)
                .Include(p => p.Detalles)
                .Where(p => p.EstadoPedido.Nombre == "PENDIENTE") // Filtro de negocio
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();
        }

        // Acción para aprobar pedido desde la Web
        public async Task<IActionResult> OnPostAprobarAsync(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                // Asumimos que ID 2 es "PAGADO" o "APROBADO" en tu BD
                // Ajusta este ID según tus datos semilla
                pedido.IdEstadoPedido = 2;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}