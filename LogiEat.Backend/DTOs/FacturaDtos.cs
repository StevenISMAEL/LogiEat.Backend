namespace LogiEat.Backend.DTOs
{
    public class FacturaResumenDto
    {
        public int IdFactura { get; set; }
        public string Fecha { get; set; }
        public string Cliente { get; set; }
        public string Ruc { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string Origen { get; set; } // "Pedido #123" o "Venta Directa"
    }

    public class FacturaDetalleDto : FacturaResumenDto
    {
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public List<DetalleItemDto> Items { get; set; } = new();
    }

    public class DetalleItemDto
    {
        public string Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}