using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LogiEat.Backend.Services;
using LogiEat.Backend.Services.Facturacion;

namespace LogiEat.Backend.Pages.Admin
{
    // Solo permitimos entrar a Administradores
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        // 1. Inyecta el servicio en el constructor
        private readonly IFacturacionService _facturacionService;
        private readonly AppDbContext _context;
        private readonly IAuditoriaService _auditoria;
        public IndexModel(AppDbContext context, IAuditoriaService auditoria, IFacturacionService facturacionService)
        {
            _context = context;
            _auditoria = auditoria;
            _facturacionService = facturacionService;
        }

        // La inicializamos como una lista vacía desde el principio
        public IList<Pedido> PedidosPendientes { get; set; } = new List<Pedido>();
        public async Task OnGetAsync()
        {
            // Cargamos los pedidos pendientes para mostrarlos en la tabla
            PedidosPendientes = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.EstadoPedido)
                .Include(p => p.Detalles)
                //.Where(p => p.EstadoPedido.Nombre == "PAGADO - ESPERANDO APROBACION") // Filtro de negocio
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAceptarAsync(int id)
        {
            try
            {
                // Una sola línea que hace todo el trabajo sucio
                var factura = await _facturacionService.GenerarFacturaPorAprobacionAsync(id);

                TempData["SuccessMessage"] = $"Pedido aprobado. Factura #{factura.IdFactura} generada.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRechazarAsync(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                // Buscamos el estado: "CANCELADO" o "RECHAZADO"
                var estadoCancelado = await _context.EstadoPedidos
                    .FirstOrDefaultAsync(e => e.Nombre.Contains("CANCELADO") || e.Nombre.Contains("RECHAZADO"));

                if (estadoCancelado != null)
                {
                    pedido.IdEstadoPedido = estadoCancelado.IdEstadoPedido;

                    // Opcional: Aquí podrías lógica para anular la factura si quisieras
                    var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.IdPedido == id);
                    if (factura != null) factura.Estado = "ANULADA";

                    await _context.SaveChangesAsync();

                    await _auditoria.RegistrarEvento("Rechazar Pedido", "Pedido", id, "El Admin rechazó el pedido.");
                }
            }
            return RedirectToPage();
        }

        // Handler 1: De Cocina -> A la Moto 🛵
        public async Task<IActionResult> OnPostEnviarDeliveryAsync(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                var estadoCamino = await _context.EstadoPedidos.FirstOrDefaultAsync(e => e.Nombre == "EN CAMINO");
                if (estadoCamino != null)
                {
                    pedido.IdEstadoPedido = estadoCamino.IdEstadoPedido;
                    await _context.SaveChangesAsync();
                    await _auditoria.RegistrarEvento("Delivery", "Pedido", id, "Pedido despachado con el repartidor.");
                }
            }
            return RedirectToPage();
        }

        // Handler 2: De la Moto -> Al Cliente (Fin) ✅
        public async Task<IActionResult> OnPostConfirmarEntregaAsync(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                var estadoEntregado = await _context.EstadoPedidos.FirstOrDefaultAsync(e => e.Nombre == "ENTREGADO");
                if (estadoEntregado != null)
                {
                    pedido.IdEstadoPedido = estadoEntregado.IdEstadoPedido;
                    await _context.SaveChangesAsync();
                    await _auditoria.RegistrarEvento("Entrega Final", "Pedido", id, "Pedido entregado al cliente satisfactoriamente.");
                }
            }
            return RedirectToPage();
        }

    }
}