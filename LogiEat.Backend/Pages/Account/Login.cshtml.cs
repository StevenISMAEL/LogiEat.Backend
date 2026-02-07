using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LogiEat.Backend.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;

        public LoginModel(SignInManager<Users> signInManager, UserManager<Users> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El correo es obligatorio.")]
            [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Limpiar cookie externa para asegurar un login limpio
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // 1. Buscamos al usuario UNA SOLA VEZ
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // Si el usuario existe, verificamos si está bloqueado
                if (user != null)
                {
                    // 2. VERIFICACIÓN DE ESTADO (El Portero 💂‍♂️)
                    if (!user.Activo)
                    {
                        ModelState.AddModelError(string.Empty, "⛔ Su cuenta ha sido desactivada. Contacte al administrador.");
                        return Page(); // Detenemos todo aquí. No se intenta el login.
                    }
                }

                // 3. Intentar Logueo (Verifica contraseña)
                // lockoutOnFailure: true es recomendable para evitar fuerza bruta, pero false está bien para desarrollo.
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // 4. LÓGICA DE REDIRECCIÓN
                    // Reutilizamos la variable 'user' que obtuvimos arriba (ya sabemos que no es null si entró aquí)

                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToPage("/Admin/Index");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Cocina"))
                    {
                        // Ejemplo: Si tienes un dashboard para cocina
                        return RedirectToPage("/Cocina/Index");
                    }
                    else
                    {
                        // Si es cliente o cualquier otro rol
                        // Verificamos si hay un ReturnUrl pendiente (ej: intentó entrar al carrito sin loguearse)
                        if (Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                        {
                            return LocalRedirect(returnUrl);
                        }
                        return RedirectToPage("/Cliente/Catalogo");
                    }
                }

                // Si llegamos aquí, la contraseña estaba mal o el usuario no existe
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return Page();
            }

            // Si el modelo no es válido (ej: campo vacío)
            return Page();
        }
    }
}