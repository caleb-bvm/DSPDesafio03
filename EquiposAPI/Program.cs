using System.Text;
using EquiposAPI.Data;
using EquiposAPI.Mapping;
using EquiposAPI.Models;
using EquiposAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var cadena = builder.Configuration.GetConnectionString("EquiposDb")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:EquiposDb en User Secrets o variables de entorno.");
var claveJwt = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(claveJwt) || Encoding.UTF8.GetByteCount(claveJwt) < 32)
    throw new InvalidOperationException("Configure Jwt:Key con al menos 32 caracteres en User Secrets o variables de entorno.");

var emisor = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Falta Jwt:Issuer.");
var audiencia = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Falta Jwt:Audience.");
if (builder.Configuration.GetValue<int>("Jwt:MinutesToExpire") <= 0)
    throw new InvalidOperationException("Jwt:MinutesToExpire debe ser mayor que cero.");

builder.Services.AddDbContext<EquiposDbContext>(opciones => opciones.UseSqlServer(cadena));
builder.Services.AddAutoMapper(_ => { }, typeof(EquipoProfile));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddControllers().ConfigureApiBehaviorOptions(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var mensaje = contexto.ModelState.Values
            .SelectMany(estado => estado.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault() ?? "Revise los datos enviados.";
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { message = mensaje });
    };
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            ValidIssuer = emisor,
            ValidAudience = audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
        opciones.Events = new JwtBearerEvents
        {
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await contexto.Response.WriteAsJsonAsync(new { message = "Debe iniciar sesión para continuar." });
            },
            OnForbidden = async contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                await contexto.Response.WriteAsJsonAsync(new { message = "No tiene permiso para realizar esta acción." });
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo { Title = "EquiposAPI", Version = "v1" });
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue el token obtenido en /api/auth/login."
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async contexto =>
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

await AdminSeeder.InicializarAsync(app.Services, app.Configuration);
await app.RunAsync();
