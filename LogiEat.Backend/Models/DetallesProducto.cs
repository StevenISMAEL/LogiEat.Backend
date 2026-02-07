using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("detalles_producto")] // Fíjate en el nombre exacto de la tabla para el Trigger
    public class DetallesProducto
    {
        [Key]
        [Column("id_detalle")]
        public int IdDetalle { get; set; }

        public int IdProducto { get; set; }

        [Column("id_transaccion")]
        public int IdTransaccion { get; set; }

        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoEstado { get; set; } = "pedido"; // "pedido" o "compra"

        [ForeignKey("IdProducto")]
        public virtual Producto? Producto { get; set; }
    }
}