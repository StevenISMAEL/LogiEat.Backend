using LogiEat.Backend.Data; // Importante para AppDbContext
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogiEat.Backend.Pages.Admin.Usuarios
{
    [Authorize(Roles = "Admin")]
    public class EliminarModel : PageModel
    {
        // Usamos el Contexto directo para evitar problemas de conversión de IDs
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public EliminarModel(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Users UsuarioMostrar { get; set; }

        [BindProperty]
        public int IdUsuarioABorrar { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            UsuarioMostrar = await _context.Users.FindAsync(id);

            if (UsuarioMostrar == null) return NotFound();

            IdUsuarioABorrar = id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _context.Users.FindAsync(IdUsuarioABorrar);

            if (user == null) return NotFound();

            // Evitar auto-eliminación
            if (user.UserName == User.Identity.Name)
            {
                TempData["ErrorMessage"] = "⛔ No puedes eliminarte a ti mismo.";
                return RedirectToPage("./Index");
            }

            // BORRADO LÓGICO
            user.Activo = false;

            // Guardamos usando el contexto directamente
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Usuario desactivado correctamente.";
            return RedirectToPage("./Index");
        }
    }
}