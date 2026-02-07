using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    // 1. USUARIO: Hereda de IdentityUser<int>
    [Table("Usuarios")] // Mapeo exacto a tu tabla SQL
    public class Users : IdentityUser<int>
    {
        // IdentityUser ya trae UserName, Email, PasswordHash, etc.
        // Agregamos lo que falte:
        [Column("Nombre")] // Tu SQL usa "Nombre", Identity usa "UserName". Mapeamos.
        public string FullName { get; set; }
    }

    // 2. ROL: Hereda de IdentityRole<int>
    [Table("Roles")]
    public class Roles : IdentityRole<int>
    {
        public Roles() : base() { }
        public Roles(string roleName) : base(roleName) { }
    }
}