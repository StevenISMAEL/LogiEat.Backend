using LogiEat.Mobile.Services;
using LogiEat.Mobile.Models;

namespace LogiEat.Mobile.Views;

public partial class MisFacturasPage : ContentPage
{
    private readonly ApiService _apiService;

    public MisFacturasPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarFacturas();
    }

    private async Task CargarFacturas()
    {
        Refresher.IsRefreshing = true;
        var facturas = await _apiService.ObtenerMisFacturas();
        CvFacturas.ItemsSource = facturas;
        Refresher.IsRefreshing = false;
    }

    private void OnRefreshing(object sender, EventArgs e)
    {
        _ = CargarFacturas();
    }

    private async void OnFacturaTapped(object sender, TappedEventArgs e)
    {
        // Al tocar, navegamos al detalle pasando el ID
        if (e.Parameter is int idFactura)
        {
            await Navigation.PushAsync(new DetalleFacturaPage(_apiService, idFactura));
        }
    }
}