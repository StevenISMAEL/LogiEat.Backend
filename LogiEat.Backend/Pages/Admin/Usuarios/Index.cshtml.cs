using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Pages.Admin.Usuarios
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<UsuarioConRol> UsuariosLista { get; set; } = new();

        public async Task OnGetAsync()
        {
            // IMPORTANTE: Quitamos el filtro de "where u.Activo == true" 
            // para poder ver a los inactivos y reactivarlos.
            UsuariosLista = await (from u in _context.Users
                                   join ur in _context.UserRoles on u.Id equals ur.UserId into userRoles
                                   from ur in userRoles.DefaultIfEmpty()
                                   join r in _context.Roles on ur.RoleId equals r.Id into roles
                                   from r in roles.DefaultIfEmpty()
                                   select new UsuarioConRol
                                   {
                                       Id = u.Id,
                                       NombreCompleto = u.FullName,
                                       Email = u.Email,
                                       Rol = r.Name ?? "Sin Rol",
                                       Activo = u.Activo // <--- Traemos el estado
                                   }).ToListAsync();
        }

        // NUEVA FUNCIÓN: Para activar/desactivar desde el switch
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var usuario = await _context.Users.FindAsync(id);
            if (usuario != null)
            {
                // Simplemente invertimos el valor
                usuario.Activo = !usuario.Activo;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Estado de {usuario.FullName} actualizado.";
            }
            return RedirectToPage();
        }

        public class UsuarioConRol
        {
            public int Id { get; set; }
            public string NombreCompleto { get; set; }
            public string Email { get; set; }
            public string Rol { get; set; }
            public bool Activo { get; set; } // <--- Agregado
        }
    }
}