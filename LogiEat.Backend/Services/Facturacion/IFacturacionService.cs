using LogiEat.Backend.Models;

namespace LogiEat.Backend.Services.Facturacion
{
    public interface IFacturacionService
    {
        // Método para el cálculo matemático puro
        Factura CalcularFactura(Pedido pedido, List<DetallePedido> detalles, decimal descuentoMonto);

        // Método para el flujo automático de aprobación (RF-01)
        Task<Factura> GenerarFacturaPorAprobacionAsync(int idPedido);

        // [NUEVO] Método para facturación directa por vendedor (RF-02)
        Task<Factura> CrearFacturaDirectaAsync(int idCliente, List<DetallePedido> items, int idTipoPago, string ruc, string nombre);
    }
}