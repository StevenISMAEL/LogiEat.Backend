using LogiEat.Backend.Models;

namespace LogiEat.Backend.Services
{
    public class FacturacionService : IFacturacionService
    {
        private const decimal PORCENTAJE_IVA = 0.15m; // 15%

        public Factura CalcularFactura(Pedido pedido, List<DetallePedido> detalles, decimal descuentoMonto)
        {
            // Validaciones básicas
            if (pedido == null) throw new ArgumentNullException(nameof(pedido));
            if (detalles == null || !detalles.Any()) throw new ArgumentException("El pedido no tiene productos.");
            if (descuentoMonto < 0) throw new ArgumentException("El descuento no puede ser negativo.");

            // 1. CÁLCULOS
            // Usamos Math.Round para evitar problemas de decimales infinitos
            decimal subtotal = Math.Round(detalles.Sum(d => d.Cantidad * d.PrecioUnitarioSnapshot), 2);
            decimal montoIva = Math.Round(subtotal * PORCENTAJE_IVA, 2);

            // Calculamos el total restando el descuento (aunque no lo guardemos en la DB, afecta al total a pagar)
            decimal totalCalculado = (subtotal + montoIva) - descuentoMonto;

            // Validación de seguridad: El total no puede ser negativo
            if (totalCalculado < 0) totalCalculado = 0;

            // 2. CREACIÓN DEL OBJETO (Solo con campos que SÍ tienes)
            return new Factura
            {
                // CORRECCIÓN 1: Usamos la propiedad que seguramente tienes: 'IdPedido'
                // Si en tu modelo se llama de otra forma (ej: PedidoId), cámbialo aquí.
                IdPedido = pedido.IdPedido,

                FechaEmision = DateTime.Now,
                Subtotal = subtotal,
                Iva = montoIva,

                // CORRECCIÓN 2: Quitamos la línea 'Descuento = ...' porque no existe en tu tabla.
                // El descuento ya se aplicó al Total en la línea de arriba.
                Total = totalCalculado,

                // Asignamos un tipo de pago por defecto o el que venga en la lógica
                IdTipoPago = 1 // Ejemplo: 1 = Efectivo (Ajusta según tus datos)
            };
        }

        public bool EsConsistente(Factura factura)
        {
            // Validación simplificada ya que no guardamos el descuento
            // Verificamos que Subtotal + IVA sea >= Total (la diferencia sería el descuento implícito)
            return (factura.Subtotal + factura.Iva) >= factura.Total;
        }
    }
}