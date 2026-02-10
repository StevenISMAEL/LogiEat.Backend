using System.Text.Json.Serialization;

namespace LogiEat.Mobile.Models
{
    public class ClienteDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }

        // Si el backend no envía RUC, este campo puede ser nulo, 
        // pero lo necesitamos para el formulario.
        public string Ruc { get; set; }

        // Helper para mostrar en el ComboBox (Picker)
        public string DisplayName => $"{Nombre} ({Email})";
    }
}