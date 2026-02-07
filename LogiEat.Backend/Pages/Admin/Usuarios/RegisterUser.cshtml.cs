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
    public class RegisterUserModel : PageModel
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<Roles> _roleManager;

        public RegisterUserModel(UserManager<Users> userManager, RoleManager<Roles> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public SelectList RolesLista { get; set; }

        public class InputModel
        {
            [Required] public string Nombre { get; set; }
            [Required, EmailAddress] public string Email { get; set; }
            [Required, MinLength(6)] public string Password { get; set; }
            [Required] public string Rol { get; set; }
        }

        public void OnGet() => RolesLista = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                RolesLista = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
                return Page();
            }

            var user = new Users { UserName = Input.Email, Email = Input.Email, FullName = Input.Nombre };
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Asignación de Rol en tu tabla UsuarioRoles
                await _userManager.AddToRoleAsync(user, Input.Rol);
                TempData["SuccessMessage"] = "Usuario creado con éxito.";
                return RedirectToPage("/Admin/Index");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            RolesLista = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return Page();
        }


   
    }
}