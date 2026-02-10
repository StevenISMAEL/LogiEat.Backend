namespace LogiEat.Mobile;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        // PASO 1: Asignar una página INMEDIATAMENTE para que Android tenga qué dibujar.
        // Por defecto, asumimos que el usuario debe loguearse.
        MainPage = serviceProvider.GetService<Views.LoginPage>();

        // PASO 2: Verificar en segundo plano si ya tiene token
        CheckLoginStatus(serviceProvider);
    }

    private async void CheckLoginStatus(IServiceProvider serviceProvider)
    {
        try
        {
            // Buscamos el token
            var token = await SecureStorage.GetAsync("auth_token");

            // Si hay token, cambiamos la página que ya pusimos (Login) por la del Home
            if (!string.IsNullOrEmpty(token))
            {
                // Usamos el Dispatcher para asegurar que el cambio visual sea seguro
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var homePage = serviceProvider.GetService<Views.HomePage>();
                    MainPage = new NavigationPage(homePage);
                });
            }
        }
        catch (Exception ex)
        {
            // Si algo falla leyendo el almacenamiento, al menos ya estamos en el Login
            Console.WriteLine($"Error leyendo token: {ex.Message}");
        }
    }
}