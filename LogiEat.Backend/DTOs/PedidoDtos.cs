namespace LogiEat.Backend.DTOs
{
    // Lo que envía el móvil para crear un pedido
    public class CrearPedidoDto
    {
        public List<ProductoItemDto> Productos { get; set; } = new();
    }

    public class ProductoItemDto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }

    // Lo que envía el móvil para registrar un pago
    public class PagoCrearDto
    {
        public int PedidoId { get; set; }
        public decimal Monto { get; set; }
        public int TipoPagoId { get; set; }
        // El estado por defecto lo pondremos en el backend (Pendiente)
    }

    // Lo que devolvemos al móvil (Lectura)
    public class PagoDto
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public decimal Monto { get; set; }
        public string TipoPago { get; set; }
        public string EstadoPago { get; set; }
        public DateTime FechaPago { get; set; }
    }
}