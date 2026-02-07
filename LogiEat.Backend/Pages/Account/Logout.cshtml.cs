using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogiEat.Backend.Pages.Account
{
    [IgnoreAntiforgeryToken]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<Users> _signInManager;

        public LogoutModel(SignInManager<Users> signInManager)
        {
            _signInManager = signInManager;
        }

        // Método POST: Es el que llama el botón "Salir"
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Cierra la sesión de Identity (Borra la cookie de autenticación)
            await _signInManager.SignOutAsync();

            // 2. Redirige al Login
            return RedirectToPage("/Account/Login");
        }

        // Método GET: Por si alguien intenta entrar directo a /Account/Logout
        public IActionResult OnGet()
        {
            return RedirectToPage("/Account/Login");
        }
    }
}