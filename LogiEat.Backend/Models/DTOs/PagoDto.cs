using LogiEat.Backend.Models;

namespace LogiEat.Backend.Models.DTOs
{
    public class PagoDto
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public decimal Monto { get; set; }
        public TipoPago? TipoPago { get; set; }
        public EstadoPago? EstadoPago { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
