using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using LogiEat.Backend.Services.Facturacion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE BASE DE DATOS
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CONFIGURACIÓN DE IDENTITY
builder.Services.AddIdentity<Users, Roles>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. CONFIGURACIÓN DE AUTENTICACIÓN (SIMPLIFICADA)
// Quitamos "SmartAuth". Dejamos que Identity maneje las Cookies por defecto.
builder.Services.AddAuthentication(options =>
{
    // Por defecto, todo es Cookie (para la Web)
    // La API usará JWT porque se lo ordenamos en los Controladores con [Authorize(Schemes=...)]
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// 4. CONFIGURACIÓN DE COOKIES (IMPORTANTE PARA RAZOR PAGES)
// Esto le dice al sistema: "Si alguien no está logueado en la Web, mándalo a /Account/Login"
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(1); // La sesión web dura 1 hora
});

// 5. SERVICIOS WEB Y API
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddRazorPages();

// 6. SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LogiEat Backend", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Ingresa 'Bearer' [espacio] y tu token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] {} }
    });
});

// 1. Necesario para obtener la IP y el Usuario dentro de los servicios
builder.Services.AddHttpContextAccessor();

// 2. Registramos nuestro nuevo servicio
builder.Services.AddScoped<LogiEat.Backend.Services.IAuditoriaService, LogiEat.Backend.Services.AuditoriaService>();
//XUNIT
builder.Services.AddScoped<IFacturacionService, FacturacionService>();

var app = builder.Build();

// --- PIPELINE ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// SEEDER AUTOMÁTICO
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error en el Seeder.");
    }
}



app.Run();