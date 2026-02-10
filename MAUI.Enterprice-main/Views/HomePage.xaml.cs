using LogiEat.Mobile.Services;
using LogiEat.Mobile.Views; // Necesario para acceder a las otras Pages

namespace LogiEat.Mobile.Views;

public partial class HomePage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public HomePage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ConfigurarVistaPorRol();
    }

    private async Task ConfigurarVistaPorRol()
    {
        var rol = await SecureStorage.GetAsync("auth_role");
        var nombre = await SecureStorage.GetAsync("auth_name");

        LblNombreUsuario.Text = nombre ?? "Usuario";
        LblRolUsuario.Text = $"Perfil: {rol?.ToUpper()}";

        if (rol == "Admin")
        {
            PanelAdmin.IsVisible = true;
            PanelCliente.IsVisible = false;
        }
        else
        {
            PanelAdmin.IsVisible = false;
            PanelCliente.IsVisible = true;
        }
    }

    // --- ACCIONES DE LOS BOTONES ---

    private async void OnNuevaEncomiendaClicked(object sender, EventArgs e)
    {
        // Por ahora lo dejamos como alerta o podrías reutilizar RealizarPedidoPage
        await DisplayAlert("Próximamente", "Módulo de paquetería en construcción.", "OK");
    }

    // 1. Cliente: Ir a pedir comida
    private async void OnPedirComidaClicked(object sender, EventArgs e)
    {
        // Creamos una nueva instancia del servicio para asegurar frescura
        await Navigation.PushAsync(new RealizarPedidoPage(new ApiService()));
    }

    // 2. Cliente: Ver historial
    private async void OnMisPedidosClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MisPedidosPage(new ApiService()));
    }

    // 3. Admin: Aprobar pedidos
    private async void OnAprobarPedidosClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminPedidosPage(new ApiService()));
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        SecureStorage.RemoveAll();
        Application.Current.MainPage = _serviceProvider.GetService<LoginPage>();
    }
    private async void OnMisFacturasClicked(object sender, EventArgs e)
    {
        // Obtenemos la página desde el contenedor de servicios para que inyecte el ApiService
        var services = Application.Current.Handler.MauiContext.Services;
        var misFacturasPage = services.GetService<MisFacturasPage>();

        await Navigation.PushAsync(misFacturasPage);
    }
    private async void OnVentaDirectaClicked(object sender, EventArgs e)
    {
        var page = Application.Current.Handler.MauiContext.Services.GetService<FacturaDirectaPage>();
        await Navigation.PushAsync(page);
    }
}