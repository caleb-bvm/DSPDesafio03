using EquiposMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session: aquí se guardan el token, el rol y el username después del login.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromHours(2);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});

// El handler necesita acceder a Session para leer el token.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiTokenHandler>();

// URL base de la API: se lee de appsettings.json (Api:BaseUrl), no se escribe en el código.
var apiUrl = new Uri(builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta Api:BaseUrl en appsettings.json."));

// Un HttpClient por servicio. AddHttpMessageHandler agrega ApiTokenHandler,
// que pone el token en CADA petición sin tocar DefaultRequestHeaders.
builder.Services.AddHttpClient<AuthService>(c => c.BaseAddress = apiUrl).AddHttpMessageHandler<ApiTokenHandler>();
builder.Services.AddHttpClient<CategoriaService>(c => c.BaseAddress = apiUrl).AddHttpMessageHandler<ApiTokenHandler>();
builder.Services.AddHttpClient<EquipoService>(c => c.BaseAddress = apiUrl).AddHttpMessageHandler<ApiTokenHandler>();
builder.Services.AddHttpClient<PrestamoService>(c => c.BaseAddress = apiUrl).AddHttpMessageHandler<ApiTokenHandler>();

var app = builder.Build();

// Fuera de Development, un error inesperado muestra una página genérica.
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Equipo}/{action=Index}/{id?}");

app.Run();
