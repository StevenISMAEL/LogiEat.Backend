using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LogiEat.Backend.Pages.Admin.Usuarios
{
    [Authorize(Roles = "Admin")]
    public class EditarModel : PageModel
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<Roles> _roleManager;

        public EditarModel(UserManager<Users> userManager, RoleManager<Roles> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public SelectList RolesDisponibles { get; set; }

        public class InputModel
        {
            public string Id { get; set; } // O int
            [Required] public string NombreCompleto { get; set; }
            [Required, EmailAddress] public string Email { get; set; }
            [Required] public string RolSeleccionado { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Obtenemos el rol actual (Identity permite múltiples, pero aquí asumimos 1 principal)
            var roles = await _userManager.GetRolesAsync(user);
            var rolActual = roles.FirstOrDefault();

            Input = new InputModel
            {
                Id = user.Id.ToString(), // Asegúrate de convertir si es int
                NombreCompleto = user.FullName,
                Email = user.Email,
                RolSeleccionado = rolActual
            };

            CargarRoles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CargarRoles();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null) return NotFound();

            // 1. Actualizar Datos Básicos
            user.FullName = Input.NombreCompleto;
            user.Email = Input.Email;
            user.UserName = Input.Email; // Opcional: Si quieres que el username sea el email

            await _userManager.UpdateAsync(user);

            // 2. Actualizar Rol
            var rolesActuales = await _userManager.GetRolesAsync(user);
            var rolViejo = rolesActuales.FirstOrDefault();

            if (rolViejo != Input.RolSeleccionado)
            {
                // Si tenía rol, lo quitamos
                if (!string.IsNullOrEmpty(rolViejo))
                {
                    await _userManager.RemoveFromRoleAsync(user, rolViejo);
                }
                // Agregamos el nuevo
                await _userManager.AddToRoleAsync(user, Input.RolSeleccionado);
            }

            TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
            return RedirectToPage("./Index");
        }

        private void CargarRoles()
        {
            RolesDisponibles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
        }
    }
}