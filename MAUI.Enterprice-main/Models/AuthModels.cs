using System.Text.Json.Serialization;

namespace LogiEat.Mobile.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public string IdUsuario { get; set; } // Recuerda que lo cambiamos a string por el GUID
        public string NombreCompleto { get; set; }
        public string Email { get; set; }

        // --- NUEVO CAMPO ---
        public string Rol { get; set; }
    }
}