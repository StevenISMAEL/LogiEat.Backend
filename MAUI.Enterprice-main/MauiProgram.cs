using LogiEat.Mobile.Services; // Asegúrate de tener estos usings
using LogiEat.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace LogiEat.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // --- ESTAS LÍNEAS SON VITALES ---
            // Si falta alguna, la app CRASHEA al iniciar
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MisFacturasPage>();
            builder.Services.AddTransient<DetalleFacturaPage>();
            builder.Services.AddTransient<FacturaDirectaPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}