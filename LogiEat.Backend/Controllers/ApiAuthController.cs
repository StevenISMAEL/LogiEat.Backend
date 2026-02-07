using LogiEat.Backend.Models;
using LogiEat.Backend.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LogiEat.Backend.Controllers
{
    [Route("api/[controller]")] // La ruta será: api/ApiAuth
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiAuthController : ControllerBase
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<Roles> _roleManager;
        private readonly IConfiguration _configuration;

        public ApiAuthController(UserManager<Users> userManager,
                                 RoleManager<Roles> roleManager,
                                 IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // POST: api/ApiAuth/Login
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Buscar usuario por Email
            var user = await _userManager.FindByEmailAsync(model.Email);

            // 2. Verificar contraseña
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // 3. Obtener Roles
                var userRoles = await _userManager.GetRolesAsync(user);

                // 4. Generar Token
                var token = GenerateJwtToken(user, userRoles);

                // 5. Responder con el JSON exacto que espera tu App MAUI
                return Ok(new
                {
                    token = token,
                    idUsuario = user.Id.ToString(), // Convertimos int a string para el JSON
                    email = user.Email,
                    nombreCompleto = user.FullName,
                    rol = userRoles.FirstOrDefault() ?? "Cliente"
                });
            }

            return Unauthorized(new { message = "Credenciales incorrectas" });
        }

        // POST: api/ApiAuth/Register
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Verificar si existe
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "El usuario ya existe." });

            // 2. Crear objeto Usuario (Notar que no asignamos Id, es automático)
            Users user = new Users()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email, // En Identity, UserName suele ser el Email
                FullName = model.Name
            };

            // 3. Guardar en BD
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error al crear usuario", errors = result.Errors });

            // 4. Asignar Rol por defecto ("Cliente")
            // Primero aseguramos que el rol exista en la BD
            if (!await _roleManager.RoleExistsAsync("Cliente"))
                await _roleManager.CreateAsync(new Roles("Cliente"));

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new Roles("Admin"));

            await _userManager.AddToRoleAsync(user, "Cliente");

            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        // MÉTODO PRIVADO: Generación de JWT
        private string GenerateJwtToken(Users user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                // 1. El SUB (Sujeto) ahora es el ID del usuario (Esto arregla el conflicto)
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), 
    
                // 2. El Email lo ponemos en su propio campo estándar
                new Claim(JwtRegisteredClaimNames.Email, user.Email),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    
                // Dejamos este por compatibilidad, aunque el sub ya hace el trabajo
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new Claim("NombreCompleto", user.FullName ?? "Usuario")
            };

            // Añadir roles como Claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(1); // Token válido por 1 día

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Solo un Admin puede crear otros Admins
        [HttpPost("CrearAdmin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CrearAdmin([FromBody] RegisterViewModel model)
        {
            var user = new Users { UserName = model.Email, Email = model.Email, FullName = model.Name };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin"); // <--- Aquí forzamos el rol
                return Ok("Administrador creado exitosamente");
            }
            return BadRequest(result.Errors);
        }

        [HttpGet("WhoAmI")]
        [Authorize] // Solo requiere estar logueado, sin rol específico
        public IActionResult WhoAmI()
        {
            var userClaims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(new
            {
                Mensaje = "Esto es lo que el servidor ve de ti:",
                Nombre = User.Identity.Name,
                EsAdmin = User.IsInRole("Admin"), // <--- ¡Esto es lo que importa!
                Claims = userClaims
            });
        }
    }
}