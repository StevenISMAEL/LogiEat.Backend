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
    public class MisFacturasModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public MisFacturasModel(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Factura> Facturas { get; set; } = new List<Factura>();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // Buscamos por el UsuarioId de la factura, no por el del pedido
            Facturas = await _context.Facturas
                .Where(f => f.UsuarioId == user.Id) // Filtro directo y eficiente
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();
        }
    }
}