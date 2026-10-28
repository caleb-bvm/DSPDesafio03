using System.Text;
using EquiposAPI.Data;
using EquiposAPI.Mapping;
using EquiposAPI.Models;
using EquiposAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Secretos. Se leen de User Secrets (o variables de entorno), nunca de
//    appsettings.json. Si falta alguno, la API se detiene con un mensaje claro.
// ---------------------------------------------------------------------------
var cadenaConexion = builder.Configuration.GetConnectionString("EquiposDb")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:EquiposDb en User Secrets.");

var claveJwt = builder.Configuration["Jwt:Key"] ?? "";
if (claveJwt.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe existir en User Secrets y tener al menos 32 caracteres.");

// ---------------------------------------------------------------------------
// 2. Servicios
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<EquiposDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));
builder.Services.AddAutoMapper(_ => { }, typeof(MappingProfile));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddControllers().ConfigureApiBehaviorOptions(opciones =>
{
    // Cuando un DTO no pasa las validaciones ([Required], [StringLength]...),
    // se responde 400 con el mismo formato que el resto de errores: { message }.
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var primerError = contexto.ModelState.Values
            .SelectMany(campo => campo.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault();
        return new BadRequestObjectResult(new { message = primerError ?? "Revise los datos enviados." });
    };
});

// Autenticación con JWT: cada petición debe traer "Authorization: Bearer <token>".
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            // Sin tolerancia: el token deja de servir justo cuando expira.
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Swagger con botón "Authorize" para pegar el token y probar los endpoints.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo { Title = "EquiposAPI", Version = "v1" });
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pegue el token obtenido en /api/auth/login."
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });
});

// ---------------------------------------------------------------------------
// 3. Pipeline HTTP
// ---------------------------------------------------------------------------
var app = builder.Build();

// Cualquier excepción no controlada devuelve 500 sin mostrar detalles internos.
app.UseExceptionHandler(error => error.Run(async contexto =>
{
    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await contexto.Response.WriteAsJsonAsync(new { message = "Ocurrió un error interno. Intente de nuevo." });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Aplica las migraciones y crea el Admin inicial si la tabla Usuarios está vacía.
await AdminSeeder.InicializarAsync(app);

app.Run();
