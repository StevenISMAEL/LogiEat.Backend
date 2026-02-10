using LogiEat.Backend.Data;
using LogiEat.Backend.Models;
using LogiEat.Backend.Services;
using LogiEat.Backend.Services.Facturacion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt; // <--- AGREGADO
using System.Text.Json.Serialization; // <--- Necesitas este using arriba

var builder = WebApplication.CreateBuilder(args);

// 🔴 FIX 1: Evitar que .NET cambie los nombres de los claims (sub, roles, etc)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Users, Roles>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    // Mantenemos la flexibilidad para Cookies y JWT
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // 🔴 FIX 2: Permitir HTTP para desarrollo móvil
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        // 🔴 FIX 3: Asegurar que el ID del usuario se lea del claim correcto
        NameClaimType = JwtRegisteredClaimNames.Sub
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context => {
            Console.WriteLine("🔴 [ERROR JWT] " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context => {
            Console.WriteLine("🟢 [OK] Usuario autenticado");
            return Task.CompletedTask;
        },
        OnChallenge = context => {
            Console.WriteLine($"🟠 [CHALLENGE] {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IFacturacionService, FacturacionService>();
builder.Services.AddScoped<IPagoService, PagoServices>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Esta línea rompe el bucle infinito
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    }); builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔴 FIX 4: Comenta esta línea solo mientras pruebas en desarrollo local
// app.UseHttpsRedirection(); 

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbSeeder.SeedRolesAndAdminAsync(services);
}

app.Run();