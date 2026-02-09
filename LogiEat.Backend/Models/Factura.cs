using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    public class Factura
    {
        [Key]
        public int IdFactura { get; set; }

        public int? IdPedido { get; set; }
        [ForeignKey("IdPedido")]
        public Pedido? Pedido { get; set; } 
        public DateTime FechaEmision { get; set; }

        public string RucCedula { get; set; }
        public string NombreCliente { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Iva { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Total { get; set; }

        public string Estado { get; set; }
        public int IdTipoPago { get; set; } // Vinculación con tu tabla TipoPago
        [ForeignKey("IdTipoPago")]
        public TipoPago TipoPago { get; set; }

        public string? ReferenciaPago { get; set; } // Para num. de comprobante o "cambio de $20"

        public List<DetalleFactura> Detalles { get; set; } = new();
        public int? UsuarioId { get; set; } // Enlace directo al cliente
        [ForeignKey("UsuarioId")]
        public Users? Usuario { get; set; }
    }
}