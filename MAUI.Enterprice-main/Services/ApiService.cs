using LogiEat.Mobile.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;

namespace LogiEat.Mobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        // ===========================================================================
        // 🏠 CONFIGURACIÓN
        // ===========================================================================

        // Revisa tu launchSettings.json en el Backend para confirmar este puerto
        private const string PORT = "5177";

        public static string BaseUrl
        {
            get
            {
                // CAMBIO AQUÍ: Usamos 127.0.0.1 en lugar de localhost
                // Esto evita el error "Refused" en Windows si hay conflicto de IPv6
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    return $"http://127.0.0.1:{PORT}/api/";

                // Para Android Emulator
                return $"http://10.0.2.2:{PORT}/api/";
            }
        }

        public ApiService()
        {
            var handler = new HttpClientHandler();
            // Ignorar errores de certificado SSL en desarrollo (importante para Android)
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler)
            {
                // ✨ MAGIA: Aquí fijamos la URL base. 
                // Ya no necesitamos llamar a GetUrl() en cada método.
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // ===========================================================================
        // 🔐 AUTENTICACIÓN
        // ===========================================================================

        public async Task<LoginResponse> Login(string email, string password)
        {
            try
            {
                // Usamos ruta relativa. HttpClient le agrega "http://.../api/" al inicio automáticamente.
                var response = await _httpClient.PostAsJsonAsync("ApiAuth/Login", new LoginRequest { Email = email, Password = password }, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN ERROR] {ex.Message}");
                return null;
            }
        }

        // ===========================================================================
        // 🍔 PEDIDOS (CLIENTE)
        // ===========================================================================

        public async Task<bool> CrearPedido(List<ProductoItemDto> productos)
        {
            try
            {
                // 1. Obtener Token
                var token = await SecureStorage.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token)) return false;

                // 2. Construir petición manualmente (Más seguro para Headers)
                // OJO: Aquí estaba el error. Quitamos GetUrl().
                var request = new HttpRequestMessage(HttpMethod.Post, "Pedidos/Crear");

                // 3. Adjuntar Token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // 4. Adjuntar JSON
                string jsonContent = JsonSerializer.Serialize(new CrearPedidoDto { Productos = productos }, _jsonOptions);
                request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // 5. Enviar
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[PEDIDO ERROR] {response.StatusCode}: {error}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PEDIDO EXCEPTION] {ex.Message}");
                return false;
            }
        }

        // ===========================================================================
        // 📦 PRODUCTOS
        // ===========================================================================

        public async Task<List<ProductoDto>> ObtenerMenuReal()
        {
            try
            {
                var response = await _httpClient.GetAsync("Productos");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ProductoDto>>(_jsonOptions);
                }
                return new List<ProductoDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MENU ERROR] {ex.Message}");
                return new List<ProductoDto>();
            }
        }

        // ===========================================================================
        // 👮 ADMINISTRACIÓN
        // ===========================================================================

        public async Task<List<PedidoLecturaDto>> ObtenerMisPedidos() => await GetPedidosGenerico("Pedidos/MisPedidos");

        public async Task<List<PedidoLecturaDto>> ObtenerPendientes() => await GetPedidosGenerico("Pedidos/Pendientes");

        public async Task<bool> CambiarEstadoPedido(int idPedido, string nuevoEstado)
        {
            try
            {
                if (!await ConfigurarToken()) return false;

                string endpoint;
                if (nuevoEstado.Contains("COCINA") || nuevoEstado.Contains("APROBADO"))
                {
                    endpoint = $"Pedidos/Aprobar/{idPedido}";
                }
                else
                {
                    endpoint = $"Pedidos/CambiarEstado/{idPedido}?nuevoEstado={nuevoEstado}";
                }

                var response = await _httpClient.PostAsync(endpoint, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN ERROR] {ex.Message}");
                return false;
            }
        }

        // ===========================================================================
        // 🛠️ HELPERS
        // ===========================================================================

        private async Task<List<PedidoLecturaDto>> GetPedidosGenerico(string endpoint)
        {
            try
            {
                if (!await ConfigurarToken()) return new List<PedidoLecturaDto>();

                var response = await _httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<PedidoLecturaDto>>(_jsonOptions);
                }
                return new List<PedidoLecturaDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET ERROR] {ex.Message}");
                return new List<PedidoLecturaDto>();
            }
        }

        // ===========================================================================
        // 📄 FACTURACIÓN
        // ===========================================================================

        public async Task<List<FacturaResumenDto>> ObtenerMisFacturas()
        {
            try
            {
                if (!await ConfigurarToken()) return new List<FacturaResumenDto>();

                var response = await _httpClient.GetAsync("Facturas/MisFacturas");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<FacturaResumenDto>>(_jsonOptions);
                }
                return new List<FacturaResumenDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FACTURAS ERROR] {ex.Message}");
                return new List<FacturaResumenDto>();
            }
        }

        public async Task<FacturaDetalleDto> ObtenerDetalleFactura(int id)
        {
            try
            {
                if (!await ConfigurarToken()) return null;

                var response = await _httpClient.GetAsync($"Facturas/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<FacturaDetalleDto>(_jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FACTURA DETALLE ERROR] {ex.Message}");
                return null;
            }
        }
        

        // ===========================================================================
        // ⚡ FACTURACIÓN DIRECTA (POS)
        // ===========================================================================

        // 1. OBTENER LISTA DE CLIENTES
        public async Task<List<ClienteDto>> ObtenerClientes()
        {
            try
            {
                if (!await ConfigurarToken()) return new List<ClienteDto>();

                // Ojo: Asegúrate que esta ruta coincida con tu backend (ApiAuth o donde lo pusiste)
                var response = await _httpClient.GetAsync("ApiAuth/Clientes");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ClienteDto>>(_jsonOptions);
                }
                return new List<ClienteDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTES ERROR] {ex.Message}");
                return new List<ClienteDto>();
            }
        }

        // 2. CREAR FACTURA (Método corregido y completo)
        public async Task<int?> CrearFacturaDirecta(int? idCliente, string ruc, string nombre, int tipoPago, List<ProductoItemDto> items)
        {
            try
            {
                if (!await ConfigurarToken()) return null;

                var dto = new
                {
                    IdCliente = idCliente, // Puede ser null si es consumidor final sin registro
                    Ruc = ruc,
                    Nombre = nombre,
                    IdTipoPago = tipoPago,
                    Items = items
                };

                var response = await _httpClient.PostAsJsonAsync("Facturas/Directa", dto, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("idFactura", out var idElement))
                    {
                        return idElement.GetInt32();
                    }
                }

                var error = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[POS ERROR] {error}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POS EXCEPTION] {ex.Message}");
                return null;
            }
        }

        private async Task<bool> ConfigurarToken()
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token)) return false;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return true;
        }


    }
}