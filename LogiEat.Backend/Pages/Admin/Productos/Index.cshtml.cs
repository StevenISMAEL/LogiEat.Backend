using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin.Productos
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Producto> Productos { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Productos = await _context.Productos
                .Include(p => p.Categoria)
                .OrderBy(p => p.NombreProducto)
                .ToListAsync();
        }


       
    }
}