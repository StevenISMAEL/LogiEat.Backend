using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace LogiEat.Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Obtener los servicios de UserManager y RoleManager
            var userManager = service.GetService<UserManager<Users>>();
            var roleManager = service.GetService<RoleManager<Roles>>();

            // 1. Crear Roles si no existen
            await CreateRoleAsync(roleManager, "Admin");
            await CreateRoleAsync(roleManager, "Cliente");

            // 2. Crear Usuario Admin si no existe
            var adminEmail = "admin@logieat.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new Users
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrador Sistema",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                // Crear usuario con password fuerte
                var result = await userManager.CreateAsync(newAdmin, "Admin123$");
                if (result.Succeeded)
                {
                    // Asignar rol Admin
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }

        private static async Task CreateRoleAsync(RoleManager<Roles> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Roles(roleName));
            }
        }
    }
}