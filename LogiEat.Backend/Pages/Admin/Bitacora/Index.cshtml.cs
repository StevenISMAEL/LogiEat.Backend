using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin.Bitacora
{
    [Authorize(Roles = "Admin")] // 🔒 SOLO ADMIN
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Models.Bitacora> Registros { get; set; } = new List<Models.Bitacora>();

        public async Task OnGetAsync()
        {
            // Traemos los últimos 100 registros para que sea rápido
            Registros = await _context.Bitacoras
                .OrderByDescending(b => b.Fecha)
                .Take(100)
                .ToListAsync();
        }
    }
}