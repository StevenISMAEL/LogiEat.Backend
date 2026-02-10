using LogiEat.Mobile.Services;
using LogiEat.Mobile.Models;
using System.Text; // Necesario para StringBuilder

namespace LogiEat.Mobile.Views;

public partial class DetalleFacturaPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly int _idFactura;
    private FacturaDetalleDto _facturaActual; // Guardamos la factura en memoria

    public DetalleFacturaPage(ApiService apiService, int idFactura)
    {
        InitializeComponent();
        _apiService = apiService;
        _idFactura = idFactura;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDetalle();
    }

    private async Task CargarDetalle()
    {
        // Guardamos el resultado en la variable de clase para usarla al imprimir
        _facturaActual = await _apiService.ObtenerDetalleFactura(_idFactura);

        if (_facturaActual != null)
        {
            LblNumero.Text = $"#{_facturaActual.IdFactura:D9}";
            LblCliente.Text = _facturaActual.Cliente;
            LblRuc.Text = _facturaActual.Ruc;
            LblSubtotal.Text = $"${_facturaActual.Subtotal:F2}";
            LblIva.Text = $"${_facturaActual.Iva:F2}";
            LblTotal.Text = $"${_facturaActual.Total:F2}";

            ContainerItems.Children.Clear();
            ContainerItems.Children.Add(new Label { Text = "DETALLE DE CONSUMO", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.Gray, Margin = new Thickness(0, 0, 0, 10) });

            foreach (var item in _facturaActual.Items)
            {
                var grid = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } },
                    Margin = new Thickness(0, 5)
                };

                grid.Add(new Label { Text = $"{item.Cantidad} x {item.Producto}", TextColor = Colors.Black }, 0, 0);
                grid.Add(new Label { Text = $"${item.Subtotal:F2}", HorizontalOptions = LayoutOptions.End, TextColor = Colors.Black }, 1, 0);

                ContainerItems.Children.Add(grid);
                ContainerItems.Children.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray, Opacity = 0.5 });
            }
        }
        else
        {
            await DisplayAlert("Error", "No se pudo cargar la factura.", "OK");
            await Navigation.PopAsync();
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // =================================================================
    // 🖨️ NUEVA LÓGICA DE IMPRESIÓN / COMPARTIR
    // =================================================================
    private async void OnImprimirClicked(object sender, EventArgs e)
    {
        if (_facturaActual == null) return;

        try
        {
            // 1. Generar el HTML
            string html = GenerarCuerpoHtml();

            // 2. Crear un archivo temporal en la caché del móvil
            string nombreArchivo = $"Factura_LogiEat_{_facturaActual.IdFactura}.html";
            string rutaArchivo = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);

            await File.WriteAllTextAsync(rutaArchivo, html);

            // 3. Compartir el archivo (Esto abre el menú nativo de Android/Windows)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartir Factura",
                File = new ShareFile(rutaArchivo)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo generar el documento: {ex.Message}", "OK");
        }
    }

    private string GenerarCuerpoHtml()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: sans-serif; padding: 20px; }");
        sb.AppendLine(".header { text-align: center; margin-bottom: 20px; }");
        sb.AppendLine(".total { font-size: 1.5em; font-weight: bold; text-align: right; color: green; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { border-bottom: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1>LogiEat S.A.</h1>");
        sb.AppendLine($"<h3>Factura Electrónica #{_facturaActual.IdFactura:D9}</h3>");
        sb.AppendLine($"<p>Fecha: {_facturaActual.Fecha}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<hr/>");
        sb.AppendLine($"<p><strong>Cliente:</strong> {_facturaActual.Cliente}</p>");
        sb.AppendLine($"<p><strong>RUC/CI:</strong> {_facturaActual.Ruc}</p>");
        sb.AppendLine($"<p><strong>Estado:</strong> {_facturaActual.Estado}</p>");

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Cant.</th><th>Producto</th><th style='text-align:right'>Total</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var item in _facturaActual.Items)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{item.Cantidad}</td>");
            sb.AppendLine($"<td>{item.Producto}</td>");
            sb.AppendLine($"<td style='text-align:right'>${item.Subtotal:F2}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div style='margin-top: 20px; text-align: right;'>");
        sb.AppendLine($"<p>Subtotal: ${_facturaActual.Subtotal:F2}</p>");
        sb.AppendLine($"<p>IVA (15%): ${_facturaActual.Iva:F2}</p>");
        sb.AppendLine($"<p class='total'>TOTAL: ${_facturaActual.Total:F2}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");

        return sb.ToString();
    }
}