using System.Text.Json.Serialization;

namespace LogiEat.Mobile.Models
{
    // Resumen para la lista
    public class FacturaResumenDto
    {
        public int IdFactura { get; set; }
        public string Fecha { get; set; } // El backend lo manda ya formateado como string
        public string Cliente { get; set; }
        public string Ruc { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string Origen { get; set; } // "Pedido #123" o "Venta Directa"

        // Propiedad visual para el color del estado
        public Color ColorEstado => Estado == "ANULADA" ? Colors.Red : Colors.Green;
    }

    // Detalle completo (Hereda del resumen)
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