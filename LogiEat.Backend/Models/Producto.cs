using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreProducto { get; set; } = string.Empty;

        [Column("Stock")] // Mapeamos a la columna Stock de tu SQL
        public int Cantidad { get; set; }

        public decimal Precio { get; set; }

        public int? CategoriaProductoId { get; set; }

        [ForeignKey("CategoriaProductoId")]
        public virtual CategoriaProducto? Categoria { get; set; }
    }
}