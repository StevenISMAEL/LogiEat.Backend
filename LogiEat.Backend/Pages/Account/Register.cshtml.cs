using LogiEat.Backend.Models;
using LogiEat.Backend.ViewModels; // Usamos tus ViewModels
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogiEat.Backend.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;

        public RegisterModel(UserManager<Users> userManager, SignInManager<Users> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } // Tu modelo exacto

        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = new Users
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FullName = Input.Name, // Mapeamos Name -> FullName
                    Activo = true // El usuario nace activo
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Asignamos rol de Cliente por defecto
                    // Asegúrate de que el rol "Cliente" exista en la BD
                    await _userManager.AddToRoleAsync(user, "Cliente");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    // Si hay una URL de retorno válida (y no es la raíz "/"), úsala.
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                    {
                        return LocalRedirect(returnUrl);
                    }
                    // Si no, por defecto al Catálogo
                    return RedirectToPage("/Account/Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Si algo falla, volvemos a mostrar la página
            return Page();
        }
    }
}