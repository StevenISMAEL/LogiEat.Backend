using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    public class DetalleFactura
    {
        [Key]
        public int IdDetalleFactura { get; set; }

        public int IdFactura { get; set; }

        [ForeignKey("IdFactura")] // Le decimos: "Usa la propiedad IdFactura de arriba"
        public Factura Factura { get; set; } // Navegación inversa

        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal SubtotalLinea { get; set; }
    }
}