using EquiposMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiTokenHandler>();

var apiUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Configure Api:BaseUrl para usar el cliente MVC.");
if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri))
    throw new InvalidOperationException("Api:BaseUrl debe ser una URL absoluta.");

builder.Services.AddHttpClient<AuthService>(client => client.BaseAddress = apiUri)
    .AddHttpMessageHandler<ApiTokenHandler>();
builder.Services.AddHttpClient<CategoriaService>(client => client.BaseAddress = apiUri)
    .AddHttpMessageHandler<ApiTokenHandler>();
builder.Services.AddHttpClient<EquipoService>(client => client.BaseAddress = apiUri)
    .AddHttpMessageHandler<ApiTokenHandler>();
builder.Services.AddHttpClient<PrestamoService>(client => client.BaseAddress = apiUri)
    .AddHttpMessageHandler<ApiTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Equipo}/{action=Index}/{id?}");

app.Run();
