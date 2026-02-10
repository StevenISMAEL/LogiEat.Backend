using System.Text.Json.Serialization;

namespace LogiEat.Mobile.Models
{
    // Lo renombramos para que tenga sentido con tu Monolito
    public class ProductoDto
    {
        // El backend envía camelCase por defecto (ej: "idProducto")

        [JsonPropertyName("idProducto")]
        public int IdProducto { get; set; }

        // TU MONOLITO ENVÍA "nombre", NO "nombreProducto"
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("stock")] // Antes era "cantidad"
        public int Stock { get; set; }

        [JsonPropertyName("precio")]
        public decimal Precio { get; set; }

        [JsonPropertyName("nombreCategoria")]
        public string NombreCategoria { get; set; }
    }
}