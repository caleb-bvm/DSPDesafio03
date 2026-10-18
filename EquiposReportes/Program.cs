using EquiposReportes.Data;
using EquiposReportes.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("EquiposAPI", client =>
{
    var url = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Falta Api:BaseUrl.");
    client.BaseAddress = new Uri(url);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EquiposReportes.Auth";
        options.Cookie.HttpOnly = true;
        options.LoginPath = "/Cuenta/Login";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("EquiposReportes")
    ?? throw new InvalidOperationException(
        "Falta ConnectionStrings:EquiposReportes. Configúrela en User Secrets.");
builder.Services.AddDbContext<EquiposDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<ReportesService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reportes}/{action=Index}/{id?}");

app.Run();
