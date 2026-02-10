using LogiEat.Mobile.Services;

namespace LogiEat.Mobile.Views;

public partial class MisPedidosPage : ContentPage
{
    private readonly ApiService _apiService;

    public MisPedidosPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPedidos();
    }

    private async Task CargarPedidos()
    {
        Refresher.IsRefreshing = true;
        var pedidos = await _apiService.ObtenerMisPedidos();
        ListaPedidos.ItemsSource = pedidos;
        Refresher.IsRefreshing = false;
    }
}