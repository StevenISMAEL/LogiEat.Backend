using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using LogiEat.Backend.Services.Facturacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LogiEat.Backend.Pages.Admin.Facturas
{
    [Authorize(Roles = "Vendedor,Admin")] // RF-07: Solo Vendedor o Admin
    public class DirectaModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IFacturacionService _facturacionService;

        public DirectaModel(AppDbContext context, IFacturacionService facturacionService)
        {
            _context = context;
            _facturacionService = facturacionService;
        }

        public List<Producto> Productos { get; set; }
        public SelectList ClientesLista { get; set; }

        public async Task OnGetAsync()
        {
            Productos = await _context.Productos.Where(p => p.Cantidad > 0).ToListAsync();

            // RF-03: Asociación obligatoria a cliente registrado
            var usuarios = await _context.Users.Where(u => u.Activo).ToListAsync();
            ClientesLista = new SelectList(usuarios, "Id", "FullName");
        }

        public async Task<IActionResult> OnPostGenerarAsync(
            int idCliente,
            string ruc,
            string nombre,
            int idTipoPago,
            string cartJson)
        {
            if (string.IsNullOrEmpty(cartJson)) return Page();

            var itemsRaw = JsonSerializer.Deserialize<List<ItemFacturaDirecta>>(cartJson);

            // Convertir a DetallePedido para reutilizar la lógica del servicio
            var detalles = itemsRaw.Select(i => new DetallePedido
            {
                IdProducto = i.Id,
                NombreProductoSnapshot = i.Nombre,
                PrecioUnitarioSnapshot = i.Precio,
                Cantidad = i.Cantidad
            }).ToList();

            try
            {
                // RNF-04: Operación transaccional
                var factura = await _facturacionService.CrearFacturaDirectaAsync(idCliente, detalles, idTipoPago, ruc, nombre);
                TempData["SuccessMessage"] = $"Factura Directa #{factura.IdFactura} generada con éxito.";
                return RedirectToPage("/Cliente/VerFactura", new { id = factura.IdFactura });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }

        public class ItemFacturaDirecta
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
        }
    }
}