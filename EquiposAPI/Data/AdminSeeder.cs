using EquiposAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Data;

public static class AdminSeeder
{
    public static async Task InicializarAsync(IServiceProvider servicios, IConfiguration configuracion)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EquiposDbContext>();
        await db.Database.MigrateAsync();

        if (await db.Usuarios.AnyAsync())
            return;

        var username = configuracion["SeedAdmin:Username"]?.Trim();
        var email = configuracion["SeedAdmin:Email"]?.Trim();
        var password = configuracion["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException(
                "Configure SeedAdmin:Username, SeedAdmin:Email y SeedAdmin:Password (mínimo 8 caracteres) antes de iniciar.");
        }

        var admin = new Usuario { Username = username, Email = email, Rol = Roles.Admin };
        var hasher = alcance.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
