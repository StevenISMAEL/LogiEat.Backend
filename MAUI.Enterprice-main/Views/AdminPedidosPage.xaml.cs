using LogiEat.Mobile.Models;
using LogiEat.Mobile.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace LogiEat.Mobile.Views;

public partial class AdminPedidosPage : ContentPage
{
    private readonly ApiService _apiService;

    public ObservableCollection<PedidoLecturaDto> PedidosNuevos { get; set; } = new();
    public ObservableCollection<PedidoLecturaDto> PedidosEnCocina { get; set; } = new();

    public AdminPedidosPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        CvPorAtender.ItemsSource = PedidosNuevos;
        CvProcesados.ItemsSource = PedidosEnCocina;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatos();
    }

    private async Task CargarDatos()
    {
        var todos = await _apiService.ObtenerPendientes();

        PedidosNuevos.Clear();
        PedidosEnCocina.Clear();

        foreach (var p in todos)
        {
            // Filtro visual para separar las pestañas
            if (p.Estado.Contains("ESPERANDO") || p.Estado.Contains("PAGADO") || p.Estado == "PENDIENTE")
            {
                PedidosNuevos.Add(p);
            }
            else
            {
                PedidosEnCocina.Add(p);
            }
        }
    }

    private void OnTabPorAtenderClicked(object sender, EventArgs e)
    {
        CvPorAtender.IsVisible = true;
        CvProcesados.IsVisible = false;
        BtnPorAtender.BackgroundColor = Color.FromArgb("#6f42c1");
        BtnPorAtender.TextColor = Colors.White;
        BtnProcesados.BackgroundColor = Colors.Transparent;
        BtnProcesados.TextColor = Colors.Gray;
    }

    private void OnTabProcesadosClicked(object sender, EventArgs e)
    {
        CvPorAtender.IsVisible = false;
        CvProcesados.IsVisible = true;
        BtnProcesados.BackgroundColor = Color.FromArgb("#6f42c1");
        BtnProcesados.TextColor = Colors.White;
        BtnPorAtender.BackgroundColor = Colors.Transparent;
        BtnPorAtender.TextColor = Colors.Gray;
    }

    // --- AQUÍ ESTÁ LA CORRECCIÓN ---
    private async void OnAprobarClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        int id = (int)btn.CommandParameter;

        // CAMBIO IMPORTANTE: Enviamos "APROBADO" en lugar de "EN PREPARACION".
        // Esto le indica al ApiService que debe llamar al endpoint 'Pedidos/Aprobar/{id}'
        // el cual ejecuta la lógica de Facturación en el servidor.
        bool ok = await _apiService.CambiarEstadoPedido(id, "APROBADO");

        if (ok)
        {
            await DisplayAlert("✅ Factura Generada", $"El pedido #{id} ha sido facturado y enviado a cocina.", "OK");
            await CargarDatos(); // Recargar para moverlo de pestaña
        }
        else
        {
            await DisplayAlert("Error", "No se pudo generar la factura. Verifica si ya fue procesado.", "OK");
        }
    }

    private async void OnRechazarClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        int id = (int)btn.CommandParameter;

        bool confirmar = await DisplayAlert("Confirmar", "¿Rechazar este pedido?", "Sí", "No");
        if (!confirmar) return;

        bool ok = await _apiService.CambiarEstadoPedido(id, "RECHAZADO");
        if (ok)
        {
            await CargarDatos();
        }
    }

    private async void OnVerDetalleTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is PedidoLecturaDto pedido)
        {
            string detalles = string.Join("\n", pedido.Detalles.Select(d => d.Descripcion));
            await DisplayAlert($"Detalle Pedido #{pedido.IdPedido}", detalles, "Cerrar");
        }
    }
}