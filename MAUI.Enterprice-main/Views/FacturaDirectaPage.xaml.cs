using LogiEat.Mobile.Models;
using LogiEat.Mobile.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace LogiEat.Mobile.Views;

public partial class FacturaDirectaPage : ContentPage
{
    private readonly ApiService _apiService;
    public ObservableCollection<ProductoVista> Menu { get; set; } = new();
    private List<ProductoItemDto> _carrito = new();

    // Almacena el ID si se seleccionó del ComboBox
    private int? _idClienteSeleccionado = null;

    public FacturaDirectaPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        CvProductos.ItemsSource = Menu;
        CargarDatosIniciales();
    }

    private async void CargarDatosIniciales()
    {
        // 1. Cargar Menú
        var productos = await _apiService.ObtenerMenuReal();
        if (productos != null)
        {
            foreach (var p in productos)
            {
                Menu.Add(new ProductoVista
                {
                    Id = p.IdProducto,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    PrecioDisplay = $"${p.Precio:F2}"
                });
            }
        }

        // 2. Cargar Clientes para el ComboBox
        var clientes = await _apiService.ObtenerClientes();
        if (clientes != null)
        {
            PkrClientes.ItemsSource = clientes;
        }
    }

    // Evento al seleccionar en el ComboBox
    private void OnClienteSeleccionado(object sender, EventArgs e)
    {
        if (PkrClientes.SelectedItem is ClienteDto cliente)
        {
            _idClienteSeleccionado = cliente.Id;
            TxtNombre.Text = cliente.Nombre;
            TxtRuc.Text = string.IsNullOrEmpty(cliente.Ruc) ? "9999999999999" : cliente.Ruc;
        }
        else
        {
            _idClienteSeleccionado = null; // Caso Consumidor Final manual o selección limpia
        }
    }

    private void OnAgregarProductoClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var prod = btn.CommandParameter as ProductoVista;

        // Lógica de carrito simple
        var item = _carrito.FirstOrDefault(x => x.IdProducto == prod.Id);
        if (item != null) item.Cantidad++;
        else _carrito.Add(new ProductoItemDto { IdProducto = prod.Id, Nombre = prod.Nombre, Cantidad = 1, Precio = prod.Precio });

        ActualizarTotal();

        // Animación visual pequeña
        btn.Text = $"OK ({(_carrito.FirstOrDefault(x => x.IdProducto == prod.Id)?.Cantidad)})";
        btn.BackgroundColor = Colors.Orange;
    }

    private void ActualizarTotal()
    {
        decimal total = _carrito.Sum(x => x.Precio * x.Cantidad);
        decimal totalConIva = total * 1.15m;

        LblTotal.Text = $"${totalConIva:F2}";
        LblItemsCount.Text = $"{_carrito.Sum(x => x.Cantidad)} items";
    }

    private async void OnFacturarClicked(object sender, EventArgs e)
    {
        if (!_carrito.Any())
        {
            await DisplayAlert("Vacío", "Agrega productos primero.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(TxtRuc.Text) || string.IsNullOrEmpty(TxtNombre.Text))
        {
            await DisplayAlert("Datos", "Ingresa RUC y Nombre del cliente.", "OK");
            return;
        }

        bool confirmar = await DisplayAlert("Confirmar", $"¿Generar factura por {LblTotal.Text}?", "Sí", "No");
        if (!confirmar) return;

        // Mapeo del índice del Picker a ID de BD (1, 2, 3)
        int tipoPago = PkrPago.SelectedIndex + 1;

        // --- LLAMADA CORREGIDA ---
        var idFactura = await _apiService.CrearFacturaDirecta(
            _idClienteSeleccionado, // int? idCliente
            TxtRuc.Text,            // string ruc
            TxtNombre.Text,         // string nombre
            tipoPago,               // int tipoPago
            _carrito                // List<ProductoItemDto> items
        );

        if (idFactura.HasValue)
        {
            await DisplayAlert("Éxito", $"Factura #{idFactura} generada correctamente.", "OK");

            // Opción: Ir directamente a ver la factura generada para imprimirla
            bool ver = await DisplayAlert("Imprimir", "¿Deseas ver la factura ahora?", "Sí", "No");
            if (ver)
            {
                await Navigation.PushAsync(new DetalleFacturaPage(_apiService, idFactura.Value));
            }

            // Limpiar formulario para nueva venta
            _carrito.Clear();
            ActualizarTotal();
            TxtRuc.Text = "9999999999999";
            TxtNombre.Text = "CONSUMIDOR FINAL";
            PkrClientes.SelectedIndex = -1; // Reset del combo
            _idClienteSeleccionado = null;

            // Reset visual de botones (opcional, requeriría recargar la lista Menu)
        }
        else
        {
            await DisplayAlert("Error", "No se pudo facturar. Revisa el stock o la conexión.", "OK");
        }
    }
}