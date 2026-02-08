using LogiEat.Backend.Models;

namespace LogiEat.Backend.Services
{
    public interface IFacturacionService
    {
        // El método estrella para las pruebas unitarias
        Factura CalcularFactura(Pedido pedido, List<DetallePedido> detalles, decimal descuentoMonto);

        // Validación de consistencia
        bool EsConsistente(Factura factura);
    }
}