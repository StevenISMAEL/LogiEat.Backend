using System.ComponentModel.DataAnnotations;

namespace LogiEat.Backend.DTOs
{
    public class ProductoDto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }       // Mapea a NombreProducto
        public decimal Precio { get; set; }
        public int Stock { get; set; }           // Mapea a Cantidad
        public int? IdCategoria { get; set; }
        public string NombreCategoria { get; set; }
    }

    public class ProductoCrearDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }

        [Required]
        [Range(0.01, 10000, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required]
        public int StockInicial { get; set; }

        [Required]
        public int IdCategoria { get; set; }
    }

    // Para llenar el dropdown en la Web
    public class CategoriaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }
}