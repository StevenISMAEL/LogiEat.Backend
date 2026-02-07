using LogiEat.Backend.Data;
using LogiEat.Backend.DTOs;
using LogiEat.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiEat.Backend.Services
{
    // Interfaz (Cópiala en un archivo IPagoService.cs o aquí mismo arriba)
    public interface IPagoService
    {
        Task<Pago> RegistrarPagoAsync(PagoCrearDto dto);
        Task<IEnumerable<PagoDto>> ObtenerPagosPorPedidoAsync(int pedidoId);
    }

    public class PagoServices : IPagoService
    {
        private readonly AppDbContext _context;

        public PagoServices(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Pago> RegistrarPagoAsync(PagoCrearDto dto)
        {
            // 1. VALIDACIÓN DIRECTA (Sin HTTP)
            // Verificamos si el pedido existe en la misma BD
            var pedidoExiste = await _context.Pedidos.AnyAsync(p => p.IdPedido == dto.PedidoId);
            if (!pedidoExiste)
                throw new Exception("El Pedido no existe.");

            // 2. CREAR ENTIDAD
            var pago = new Pago
            {
                PedidoId = dto.PedidoId,
                Monto = dto.Monto,
                TipoPagoId = dto.TipoPagoId,
                EstadoPagoId = 1, // 1 = Pendiente (Según tus seeds)
                FechaPago = DateTime.Now
            };

            // 3. GUARDAR
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            return pago;
        }

        public async Task<IEnumerable<PagoDto>> ObtenerPagosPorPedidoAsync(int pedidoId)
        {
            return await _context.Pagos
                .Where(p => p.PedidoId == pedidoId)
                .Include(p => p.TipoPago)
                .Include(p => p.EstadoPago)
                .Select(p => new PagoDto
                {
                    Id = p.IdPago,
                    PedidoId = p.PedidoId,
                    Monto = p.Monto,
                    TipoPago = p.TipoPago.Nombre,
                    EstadoPago = p.EstadoPago.Nombre,
                    FechaPago = p.FechaPago
                })
                .ToListAsync();
        }
    }
}