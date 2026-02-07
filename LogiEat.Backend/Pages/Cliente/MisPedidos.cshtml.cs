using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Cliente
{
    [Authorize] // Solo usuarios logueados
    public class MisPedidosModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public MisPedidosModel(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Pedido> Pedidos { get; set; } = new List<Pedido>();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // Consultamos los pedidos DEL USUARIO ACTUAL
            Pedidos = await _context.Pedidos
                .Include(p => p.EstadoPedido) // Para ver si está "Pendiente" o "Entregado"
                .Include(p => p.Detalles)     // Para ver qué pidió (Hamburguesa x2, etc.)
                .Where(p => p.UsuarioId == user.Id)
                .OrderByDescending(p => p.FechaPedido) // Los más recientes primero
                .ToListAsync();
        }
    }
}