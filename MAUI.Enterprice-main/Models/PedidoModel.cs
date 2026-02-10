using System.Collections.Generic;

namespace LogiEat.Mobile.Models
{
    // DTO para CREAR (Enviar al servidor)
    public class CrearPedidoDto
    {
        public List<ProductoItemDto> Productos { get; set; } = new List<ProductoItemDto>();
    }

    public class ProductoItemDto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }

    // --- NUEVO: DTO PARA LEER (Recibir del servidor) ---
    public class PedidoLecturaDto
    {
        public int IdPedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public List<DetalleLecturaDto> Detalles { get; set; }

        // Propiedad visual para cambiar color según estado en la lista
        public Color ColorEstado => Estado switch
        {
            "PENDIENTE_PAGO" => Colors.Orange,
            "PAGADO" => Colors.Blue,
            "APROBADO" => Colors.Green,
            "RECHAZADO" => Colors.Red,
            _ => Colors.Gray
        };
    }

    public class DetalleLecturaDto
    {
        public string NombreProductoSnapshot { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }

        // Texto formateado para mostrar en la lista (Ej: "2x Hamburguesa")
        public string Descripcion => $"{Cantidad}x {NombreProductoSnapshot}";
    }
}