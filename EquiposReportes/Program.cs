using EquiposReportes.Data;
using EquiposReportes.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cliente HTTP para iniciar sesión contra /api/auth/login. La URL está en appsettings.json.
builder.Services.AddHttpClient("EquiposAPI", client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!));

// Después de validar el login en la API, se guarda una cookie con el usuario y su rol.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    });
builder.Services.AddAuthorization();

// Base de datos primero: el contexto se generó con Scaffold-DbContext.
// La cadena de conexión (usuario SQL "reportes", solo db_datareader) está en
// User Secrets, no en OnConfiguring ni en appsettings.json.
var cadenaConexion = builder.Configuration.GetConnectionString("EquiposReportes")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:EquiposReportes en User Secrets.");
builder.Services.AddDbContext<EquiposDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));
builder.Services.AddScoped<ReportesService>();

var app = builder.Build();

// Un error inesperado muestra una página genérica, también en Development:
// el usuario nunca ve detalles internos (pila de llamadas, mensajes de .NET).
app.UseExceptionHandler("/Home/Error");

// Una respuesta 4xx o 5xx sin contenido (ruta inexistente...) muestra la misma página.
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reportes}/{action=Index}/{id?}");

app.Run();
