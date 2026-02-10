using LogiEat.Mobile.Models;
using LogiEat.Mobile.Services;
using System.Collections.ObjectModel;
using System.Diagnostics; // Para los logs

namespace LogiEat.Mobile.Views;

public partial class RealizarPedidoPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<ProductoItemDto> _carrito = new List<ProductoItemDto>();

    public ObservableCollection<ProductoVista> ProductosMenu { get; set; } = new ObservableCollection<ProductoVista>();

    public RealizarPedidoPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        // Vinculamos la lista visual ANTES de cargar
        CvProductos.ItemsSource = ProductosMenu;

        CargarMenuReal();
    }

    private async void CargarMenuReal()
    {
        try
        {
            // CAMBIO 1: El tipo de dato ahora es ProductoDto (el que definimos en el Paso 1)
            var productosRaw = await _apiService.ObtenerMenuReal();

            Debug.WriteLine($"[VISTA] Productos recibidos: {productosRaw?.Count ?? 0}");

            if (productosRaw != null && productosRaw.Count > 0)
            {
                ProductosMenu.Clear();

                foreach (var p in productosRaw)
                {
                    var itemVisual = new ProductoVista
                    {
                        Id = p.IdProducto,

                        // CAMBIO 2: Ahora mapeamos desde 'Nombre', no 'NombreProducto'
                        // (porque así lo definimos en el DTO para coincidir con el Monolito)
                        Nombre = p.Nombre,

                        Precio = p.Precio,
                        PrecioDisplay = $"${p.Precio:F2}",

                        // CAMBIO 3: Usamos p.Nombre para buscar la imagen
                        ImagenUrl = ObtenerImagenPorNombre(p.Nombre)
                    };

                    ProductosMenu.Add(itemVisual);
                }
            }
            else
            {
                await DisplayAlert("Alerta", "El menú está vacío. Asegúrate de tener productos creados en la base de datos local.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR VISTA] {ex.Message}");
            await DisplayAlert("Error", "Error de conexión con el Monolito: " + ex.Message, "OK");
        }
    }

    // --- Helper de Imágenes (Igual que antes) ---
    private string ObtenerImagenPorNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return "https://cdn-icons-png.flaticon.com/512/1377/1377194.png";
        string n = nombre.ToLower();
        if (n.Contains("pizza")) return "https://cdn-icons-png.flaticon.com/512/3132/3132693.png";
        if (n.Contains("hamburguesa") || n.Contains("burger")) return "https://cdn-icons-png.flaticon.com/512/3075/3075977.png";
        if (n.Contains("cola") || n.Contains("bebida")) return "https://cdn-icons-png.flaticon.com/512/2405/2405597.png";

        return "https://cdn-icons-png.flaticon.com/512/1377/1377194.png";
    }

    // --- Eventos de Botones (Igual que antes) ---
    private void OnProductoAgregado(object sender, EventArgs e)
    {
        var boton = sender as Button;
        var producto = boton.CommandParameter as ProductoVista;

        var itemExistente = _carrito.FirstOrDefault(p => p.IdProducto == producto.Id);
        if (itemExistente != null) itemExistente.Cantidad++;
        else _carrito.Add(new ProductoItemDto { IdProducto = producto.Id, Nombre = producto.Nombre, Precio = producto.Precio, Cantidad = 1 });

        ActualizarResumen();

        boton.Text = "¡AGREGADO!";
        boton.BackgroundColor = Colors.Green;
        Dispatcher.StartTimer(TimeSpan.FromSeconds(0.5), () => {
            boton.Text = "AGREGAR";
            boton.BackgroundColor = Color.FromArgb("#ffc107");
            return false;
        });
    }

    private void ActualizarResumen()
    {
        decimal total = _carrito.Sum(x => x.Precio * x.Cantidad);
        LblTotal.Text = $"${total:F2}";
        LblItems.Text = $"{_carrito.Sum(x => x.Cantidad)} ítems";
    }

    private async void OnEnviarPedidoClicked(object sender, EventArgs e)
    {
        if (_carrito.Count == 0) return;
        bool exito = await _apiService.CrearPedido(_carrito);
        if (exito) { await DisplayAlert("Éxito", "Pedido enviado", "OK"); await Navigation.PopAsync(); }
        else await DisplayAlert("Error", "Fallo al enviar", "OK");
    }
}

// Clase Visual Auxiliar
public class ProductoVista
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public decimal Precio { get; set; }
    public string PrecioDisplay { get; set; }
    public string ImagenUrl { get; set; }
}