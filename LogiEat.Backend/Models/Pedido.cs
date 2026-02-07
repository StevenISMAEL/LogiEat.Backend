using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("Pedidos")]
    public class Pedido
    {
        [Key]
        public int IdPedido { get; set; }

        [Required]
        [Column("UsuarioId")]
        public int UsuarioId { get; set; } // Ajustado a INT para el Monolito

        public DateTime FechaPedido { get; set; }

        public decimal Total { get; set; }

        public int? UsuarioAdminAprobadorId { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? IdTransaccionPago { get; set; }

        [Required]
        public int IdEstadoPedido { get; set; }

        // Propiedades de navegación
        [ForeignKey("IdEstadoPedido")]
        public virtual EstadoPedido? EstadoPedido { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Users? Usuario { get; set; }

        public virtual ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }
}