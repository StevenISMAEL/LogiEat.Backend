using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("Pagos")]
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }

        // FK hacia Pedidos
        [Required]
        public int PedidoId { get; set; }

        // El monto que se pagó
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Monto { get; set; }

        // FK hacia TipoPago (Efectivo, Tarjeta...)
        [Required]
        public int TipoPagoId { get; set; }

        // FK hacia EstadoPago (Pendiente, Completado...)
        [Required]
        public int EstadoPagoId { get; set; }

        public DateTime FechaPago { get; set; }

        // --- Propiedades de Navegación (Relaciones) ---

        [ForeignKey("PedidoId")]
        public virtual Pedido? Pedido { get; set; }

        [ForeignKey("TipoPagoId")]
        public virtual TipoPago? TipoPago { get; set; }

        [ForeignKey("EstadoPagoId")]
        public virtual EstadoPago? EstadoPago { get; set; }
    }
}