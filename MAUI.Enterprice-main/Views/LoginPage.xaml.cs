using LogiEat.Mobile.Services;
using System.Diagnostics;

namespace LogiEat.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // 1. Bloquear UI mientras carga
        if (sender is Button btn) btn.IsEnabled = false;
        LoadingSpinner.IsVisible = true;
        LoadingSpinner.IsRunning = true;

        // 2. Llamada a la API
        var respuesta = await _apiService.Login(TxtEmail.Text, TxtPassword.Text);

        // 3. Desbloquear UI
        LoadingSpinner.IsRunning = false;
        LoadingSpinner.IsVisible = false;
        if (sender is Button btnRestore) btnRestore.IsEnabled = true;

        if (respuesta != null)
        {
            // --- NUEVO: LÓGICA DE DEBUGGING (Para copiar Token) ---
            Debug.WriteLine("⬇️⬇️⬇️ COPIA ESTE TOKEN ⬇️⬇️⬇️");
            Debug.WriteLine(respuesta.Token);
            Debug.WriteLine("⬆️⬆️⬆️ FIN DEL TOKEN ⬆️⬆️⬆️");

            // Copiar al portapapeles automáticamente
            await Clipboard.Default.SetTextAsync(respuesta.Token);

            // Avisar al usuario (puedes comentar esta línea si te molesta después)
            await DisplayAlert("Token Capturado", "Token copiado al portapapeles. ¡Úsalo en Swagger!", "OK");
            // ------------------------------------------------------

            // 4. Guardar datos de sesión persistentes
            await SecureStorage.SetAsync("auth_token", respuesta.Token);

            // Guardar Rol (Si viene nulo, asumimos "Cliente")
            string rolGuardar = respuesta.Rol ?? "Cliente";
            await SecureStorage.SetAsync("auth_role", rolGuardar);

            // Guardar Nombre (para el saludo "Hola Juan")
            if (!string.IsNullOrEmpty(respuesta.NombreCompleto))
                await SecureStorage.SetAsync("auth_name", respuesta.NombreCompleto);

            Debug.WriteLine($"[LOGIN] Rol guardado: {rolGuardar}");

            // 5. Navegar al Home (Inyectando servicios)
            Application.Current.MainPage = new NavigationPage(new HomePage(Application.Current.Handler.MauiContext.Services));
        }
        else
        {
            await DisplayAlert("Error", "Credenciales incorrectas", "Ok");
        }
    }
}