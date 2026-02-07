using LogiEat.Backend.Data;
using LogiEat.Backend.Models;

namespace LogiEat.Backend.Services
{
    // Interfaz para poder inyectarlo
    public interface IAuditoriaService
    {
        Task RegistrarEvento(string accion, string entidad, int idEntidad, string descripcion);
    }

    public class AuditoriaService : IAuditoriaService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegistrarEvento(string accion, string entidad, int idEntidad, string descripcion)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var log = new Bitacora
                {
                    Fecha = DateTime.Now,
                    // Detecta el usuario automáticamente (si está logueado)
                    Usuario = httpContext?.User?.Identity?.Name ?? "Sistema/Anónimo",

                    // Detecta la IP automáticamente
                    Ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Localhost",

                    Accion = accion,
                    Entidad = entidad,
                    IdEntidad = idEntidad,
                    Descripcion = descripcion
                };

                _context.Bitacoras.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Si falla la auditoría, no queremos que se caiga la aplicación principal.
                // Aquí podrías loguear el error en un archivo de texto si quisieras.
            }
        }
    }
}