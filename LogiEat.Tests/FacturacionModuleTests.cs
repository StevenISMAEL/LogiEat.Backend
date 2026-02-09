using Xunit;
using FluentAssertions;
using Moq;
using LogiEat.Backend.Models;
using LogiEat.Backend.Services.Facturacion;
using LogiEat.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics; // <--- IMPRESCINDIBLE
using LogiEat.Backend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogiEat.Tests
{
    public class FacturacionModuleTests
    {
        private readonly Mock<IAuditoriaService> _auditMock;
        private readonly AppDbContext _context;
        private readonly FacturacionService _service;

        public FacturacionModuleTests()
        {
            // --- AQUÍ ESTÁ LA SOLUCIÓN DEFINITIVA ---
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // BD única por test

                // ESTA LÍNEA OBLIGA A IGNORAR EL ERROR DE TRANSACCIÓN
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))

                .Options;
            // ----------------------------------------

            _context = new AppDbContext(options);
            _auditMock = new Mock<IAuditoriaService>();

            // Instanciamos el servicio real con la BD simulada
            _service = new FacturacionService(_context, _auditMock.Object);
        }

        [Fact]
        public async Task GenerarFactura_AlAprobarPedido_DebeCrearFactura()
        {
            // Arrange
            // 1. CREAMOS EL USUARIO (Vital para que el Include(x => x.Usuario) no falle)
            var usuario = new Users { Id = 10, UserName = "juan", Email = "juan@test.com", FullName = "Juan Perez" };
            _context.Users.Add(usuario);

            // 2. CREAMOS EL ESTADO
            var estado = new EstadoPedido { IdEstadoPedido = 2, Nombre = "EN PREPARACION" };
            _context.EstadoPedidos.Add(estado);

            // 3. CREAMOS EL PRODUCTO
            var producto = new Producto { IdProducto = 10, NombreProducto = "Hamburguesa", Precio = 10, Cantidad = 100 };
            _context.Productos.Add(producto);

            // Guardamos estos maestros primero
            await _context.SaveChangesAsync();

            // 4. CREAMOS EL PEDIDO VINCULADO CORRECTAMENTE
            var pedido = new Pedido
            {
                IdPedido = 777, // Forzamos un ID conocido para evitar dudas
                UsuarioId = 10, // Coincide con el usuario creado
                IdEstadoPedido = 2, // Coincide con el estado creado
                FechaPedido = DateTime.Now,
                Total = 10,
                Detalles = new List<DetallePedido> {
            new DetallePedido {
                IdProducto = 10, // Coincide con producto
                Cantidad = 1,
                PrecioUnitarioSnapshot = 10,
                Subtotal = 10,
                NombreProductoSnapshot = "Hamburguesa"
            }
        }
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            // Buscamos explícitamente el ID 777 que acabamos de crear
            var result = await _service.GenerarFacturaPorAprobacionAsync(777);

            // Assert
            result.Should().NotBeNull();
            result.IdPedido.Should().Be(777);
            _context.Facturas.Count().Should().Be(1);
        }

        // TEST 2: No generación de factura si el pedido no existe
        [Fact]
        public async Task GenerarFactura_SiPedidoNoExiste_DebeLanzarExcepcion()
        {
            // Act
            Func<Task> act = async () => await _service.GenerarFacturaPorAprobacionAsync(99);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("*Pedido no encontrado*");
        }

        // TEST 3: Creación correcta de factura directa (Regla 9.3)
        [Fact]
        public async Task CrearFacturaDirecta_DebeTenerIdPedidoNulo()
        {
            // Arrange
            var items = new List<DetallePedido> {
                new DetallePedido {
                    IdProducto = 1,
                    Cantidad = 1,
                    PrecioUnitarioSnapshot = 5,
                    NombreProductoSnapshot = "Coca Cola"
                }
            };
            _context.Productos.Add(new Producto { IdProducto = 1, Cantidad = 10, NombreProducto = "Coca Cola", Precio = 5 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CrearFacturaDirectaAsync(1, items, 1, "123", "Juan");

            // Assert
            result.IdPedido.Should().BeNull();
            result.Estado.Should().Be("PAGADA");
        }

        // TEST 4: Asociación correcta cliente-factura (RF-03)
        [Fact]
        public async Task Factura_DebeEstarAsociadaAUnCliente()
        {
            // Arrange
            var items = new List<DetallePedido> {
                new DetallePedido {
                    IdProducto = 1,
                    Cantidad = 1,
                    PrecioUnitarioSnapshot = 5,
                    NombreProductoSnapshot = "Papas Fritas"
                }
            };
            _context.Productos.Add(new Producto { IdProducto = 1, Cantidad = 10, NombreProducto = "Papas Fritas", Precio = 5 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CrearFacturaDirectaAsync(55, items, 1, "123", "Juan");

            // Assert
            result.UsuarioId.Should().Be(55); // <--- Aquí verificamos el vínculo
        }

        // TEST 5: Integridad financiera (Cálculo de IVA)
        [Fact]
        public async Task Factura_DebeCalcularTotalConIvaCorrectamente()
        {
            // Arrange: Producto de $100
            var items = new List<DetallePedido> {
                new DetallePedido {
                    IdProducto = 1,
                    Cantidad = 1,
                    PrecioUnitarioSnapshot = 100,
                    NombreProductoSnapshot = "Banquete Familiar"
                }
            };
            _context.Productos.Add(new Producto { IdProducto = 1, Cantidad = 10, NombreProducto = "Banquete Familiar", Precio = 100 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CrearFacturaDirectaAsync(1, items, 1, "123", "Juan");

            // Assert: 100 + 15% IVA = 115
            result.Subtotal.Should().Be(100);
            result.Iva.Should().Be(15);
            result.Total.Should().Be(115);
            result.Detalles.Should().HaveCount(1);
        }

        // TEST 6: Validación de negocio (Lista vacía)
        [Fact]
        public async Task FacturaDirecta_SinItems_DebeLanzarError()
        {
            // Arrange
            var itemsVacio = new List<DetallePedido>();

            // Act
            Func<Task> act = async () => await _service.CrearFacturaDirectaAsync(1, itemsVacio, 1, "123", "Juan");

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
    }
}