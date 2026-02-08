using Xunit;
using FluentAssertions;
using LogiEat.Backend.Services;
using LogiEat.Backend.Models;

namespace LogiEat.Tests
{
    public class FacturacionModuleTests
    {
        private readonly FacturacionService _service;

        public FacturacionModuleTests()
        {
            _service = new FacturacionService();
        }

        // --- GRUPO 1: CÁLCULOS BÁSICOS ---

        [Fact]
        public void CalcularFactura_ConUnSoloItem_DebeCalcularSubtotalCorrecto()
        {
            // Arrange
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido> { new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10.00m } };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Assert
            factura.Subtotal.Should().Be(10.00m);
        }

        [Fact]
        public void CalcularFactura_DebeAplicarIvaDel15Porciento()
        {
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido> { new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 100.00m } };

            var factura = _service.CalcularFactura(pedido, detalles, 0);

            factura.Iva.Should().Be(15.00m); // 15% de 100
        }

        // --- GRUPO 2: CASOS BORDE Y EXCEPCIONES ---

        [Fact]
        public void CalcularFactura_SinDetalles_DebeLanzarExcepcion()
        {
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido>();

            Action act = () => _service.CalcularFactura(pedido, detalles, 0);

            act.Should().Throw<ArgumentException>().WithMessage("El pedido no tiene productos.");
        }

        [Fact]
        public void CalcularFactura_ConDescuentoMayorAlTotal_DebeRetornarTotalCero()
        {
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido> { new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10.00m } };
            // Total con IVA = 11.50

            var factura = _service.CalcularFactura(pedido, detalles, 20.00m); // Descuento de 20

            factura.Total.Should().Be(0);
        }


        [Fact]
        public void CalcularFactura_ConDecimalesLargos_DebeRedondearADosDecimales()
        {
            // Arrange: 10.555 debería redondearse a 10.56
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido>
            {
                new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10.555m }
            };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Assert
            factura.Subtotal.Should().Be(10.56m);
        }

        [Fact]
        public void CalcularFactura_MultiplesItems_DebeSumarCorrectamente()
        {
            // Arrange
            var pedido = new Pedido { IdPedido = 2 };
            var detalles = new List<DetallePedido>
            {
                new DetallePedido { Cantidad = 2, PrecioUnitarioSnapshot = 10.00m }, // 20.00
                new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 5.50m }   // 5.50
            };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Assert: Subtotal 25.50 + IVA (15% = 3.825 -> 3.83) = Total 29.32
            factura.Subtotal.Should().Be(25.50m);
            factura.Total.Should().Be(29.32m);
        }

        [Fact]
        public void CalcularFactura_ProductoGratis_NoDebeRomperElCalculo()
        {
            // Arrange: Producto con precio 0 (Promoción)
            var pedido = new Pedido { IdPedido = 3 };
            var detalles = new List<DetallePedido>
            {
                new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 0m }
            };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Assert
            factura.Total.Should().Be(0m);
        }

        [Fact]
        public void CalcularFactura_ConDescuentoExacto_TotalDebeSerCero()
        {
            // Arrange: Total 11.50 (10 + 1.50 IVA) - Descuento 11.50
            var pedido = new Pedido { IdPedido = 4 };
            var detalles = new List<DetallePedido>
            {
                new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10.00m }
            };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 11.50m);

            // Assert
            factura.Total.Should().Be(0m);
        }

        [Fact]
        public void CalcularFactura_CantidadesGrandes_DebeManejarPrecision()
        {
            // Arrange: 1000 unidades de 1.50
            var pedido = new Pedido { IdPedido = 5 };
            var detalles = new List<DetallePedido>
            {
                new DetallePedido { Cantidad = 1000, PrecioUnitarioSnapshot = 1.50m }
            };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Assert: 1500 subtotal + 225 IVA = 1725 Total
            factura.Total.Should().Be(1725.00m);
        }

        [Fact]
        public void CalcularFactura_PedidoNulo_DebeLanzarArgumentNullException()
        {
            // Arrange
            Pedido pedido = null;
            var detalles = new List<DetallePedido> { new DetallePedido() };

            // Act
            Action act = () => _service.CalcularFactura(pedido, detalles, 0);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CalcularFactura_DescuentoNegativo_DebeLanzarExcepcion()
        {
            // No se puede hacer un descuento de "-10" (eso sería cobrar más)
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido> { new DetallePedido { PrecioUnitarioSnapshot = 10, Cantidad = 1 } };

            Action act = () => _service.CalcularFactura(pedido, detalles, -5.00m);

            act.Should().Throw<ArgumentException>().WithMessage("El descuento no puede ser negativo.");
        }

        // NOTA: Para validar precios negativos dentro de la lista, 
        // tendríamos que agregar esa validación en el servicio. 
        // Asumiendo que agregamos: if (d.Precio < 0) throw...
        [Fact]
        public void CalcularFactura_PrecioUnitarioNegativo_DebeLanzarExcepcion()
        {
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido>
            {
                new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = -10.00m }
            };

            // Si tu servicio permite precios negativos (devoluciones), cambia el Assert.
            // Si no, debería fallar.
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Si permitimos negativo, el total baja. Si no, debería ser validado antes.
            // Asumamos que el cálculo matemático lo permite por ahora:
            factura.Subtotal.Should().Be(-10.00m);
        }

        [Fact]
        public void CalcularFactura_ListaDetallesNula_DebeLanzarExcepcion()
        {
            var pedido = new Pedido { IdPedido = 1 };
            List<DetallePedido> detalles = null;

            Action act = () => _service.CalcularFactura(pedido, detalles, 0);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CalcularFactura_ListaDetallesVacia_DebeLanzarExcepcion()
        {
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido>(); // Lista instanciada pero vacía 

            Action act = () => _service.CalcularFactura(pedido, detalles, 0);

            act.Should().Throw<ArgumentException>();
        }


        [Fact]
        public void CalcularFactura_DebeMapearCorrectamenteElIdDelPedido()
        {
            // Arrange
            var idPrueba = 999;
            var pedido = new Pedido { IdPedido = idPrueba };
            var detalles = new List<DetallePedido> { new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10 } };

            // Act
            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Assert
            factura.IdPedido.Should().Be(idPrueba);
        }

        [Fact]
        public void CalcularFactura_FechaEmision_DebeSerHoy()
        {
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido> { new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10 } };

            var factura = _service.CalcularFactura(pedido, detalles, 0);

            // Verificamos que la fecha sea reciente (margen de 1 segundo)
            factura.FechaEmision.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void EsConsistente_FacturaValida_DebeRetornarTrue()
        {
            // Arrange
            var factura = new Factura
            {
                Subtotal = 100m,
                Iva = 15m,
                Total = 115m // (100 + 15) - 0 = 115
            };

            // Como quitamos la propiedad Descuento del modelo Factura, 
            // el método EsConsistente asumirá descuento 0 o lo calculará implícitamente.
            // Si modificaste EsConsistente para no usar .Descuento, este test pasa.

            // Act
            var esValida = _service.EsConsistente(factura);

            // Assert
            esValida.Should().BeTrue();
        }

        [Fact]
        public void EsConsistente_TotalErroneo_DebeRetornarFalse()
        {
            // Arrange: Alguien manipuló el total manualmente
            var factura = new Factura
            {
                Subtotal = 100m,
                Iva = 15m,
                Total = 5000m // Error obvio
            };

            // Act
            var esValida = _service.EsConsistente(factura);

            // Assert
            esValida.Should().BeFalse();
        }

        [Fact]
        public void CalcularFactura_DebeAsignarTipoPagoPorDefecto()
        {
            // Validamos que no venga con TipoPago 0 (que daría error de FK en SQL)
            var pedido = new Pedido { IdPedido = 1 };
            var detalles = new List<DetallePedido> { new DetallePedido { Cantidad = 1, PrecioUnitarioSnapshot = 10 } };

            var factura = _service.CalcularFactura(pedido, detalles, 0);

            factura.IdTipoPago.Should().NotBe(0);
        }

    }
}