using EquiposAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Data;

// Se ejecuta una vez al arrancar la API.
public static class AdminSeeder
{
    public static async Task InicializarAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EquiposDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        // 1. Crea la base o aplica las migraciones pendientes.
        await db.Database.MigrateAsync();

        // 2. Si ya hay usuarios, no se hace nada más.
        if (await db.Usuarios.AnyAsync())
            return;

        // 3. Datos del Admin inicial. La contraseña viene de User Secrets
        //    (SeedAdmin:Password), nunca del código.
        var username = app.Configuration["SeedAdmin:Username"];
        var email = app.Configuration["SeedAdmin:Email"];
        var password = app.Configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException(
                "Configure SeedAdmin:Username, SeedAdmin:Email y SeedAdmin:Password (mínimo 8 caracteres).");
        }

        var admin = new Usuario { Username = username, Email = email, Rol = Roles.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
