using EquiposAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Data;

// Contexto de EF Core (código primero). Las migraciones de la carpeta
// Data/Migrations se generan a partir de esta configuración.
// Importante: si se cambia algo aquí hay que crear una migración nueva.
public class EquiposDbContext(DbContextOptions<EquiposDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Categoria: nombre único de hasta 100 caracteres + datos semilla -----
        modelBuilder.Entity<Categoria>(categoria =>
        {
            categoria.ToTable("Categorias");
            categoria.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            categoria.HasIndex(x => x.Nombre).IsUnique();

            // Semilla: estas categorías se insertan con la migración.
            categoria.HasData(
                new Categoria { Id = 1, Nombre = "Laptops" },
                new Categoria { Id = 2, Nombre = "Proyectores" },
                new Categoria { Id = 3, Nombre = "Kits de IoT" },
                new Categoria { Id = 4, Nombre = "Tarjetas Arduino" });
        });

        // ----- Equipo: código único, estado como texto y RowVersion -----
        modelBuilder.Entity<Equipo>(equipo =>
        {
            // La restricción CHECK impide guardar un estado distinto a los tres permitidos.
            equipo.ToTable("Equipos", tabla =>
                tabla.HasCheckConstraint("CK_Equipos_Estado",
                    "[Estado] IN ('Disponible', 'Prestado', 'Mantenimiento')"));
            equipo.Property(x => x.CodigoInventario).HasMaxLength(50).IsRequired();
            equipo.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            equipo.Property(x => x.Descripcion).HasMaxLength(500);
            equipo.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            // Valor por defecto en SQL Server: fecha y hora local de Centroamérica.
            equipo.Property(x => x.FechaRegistro).HasDefaultValueSql(
                "CONVERT(datetime2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'Central America Standard Time')");
            // IsRowVersion: columna rowversion + token de concurrencia.
            equipo.Property(x => x.RowVersion).IsRowVersion();
            equipo.HasIndex(x => x.CodigoInventario).IsUnique();
            equipo.HasOne(x => x.Categoria).WithMany(x => x.Equipos)
                .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });

        // ----- Usuario: username único y rol limitado a tres valores -----
        modelBuilder.Entity<Usuario>(usuario =>
        {
            usuario.ToTable("Usuarios", tabla =>
                tabla.HasCheckConstraint("CK_Usuarios_Rol",
                    "[Rol] IN ('Admin', 'Docente', 'Estudiante')"));
            usuario.Property(x => x.Username).HasMaxLength(100).IsRequired();
            usuario.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            usuario.Property(x => x.Email).HasMaxLength(256).IsRequired();
            usuario.Property(x => x.Rol).HasMaxLength(20).IsRequired();
            usuario.HasIndex(x => x.Username).IsUnique();
        });

        // ----- Prestamo: relaciones con Equipo y Usuario -----
        modelBuilder.Entity<Prestamo>(prestamo =>
        {
            prestamo.ToTable("Prestamos");
            prestamo.Property(x => x.Observaciones).HasMaxLength(500);
            // Índice único filtrado: un equipo solo puede tener UN préstamo activo
            // (FechaDevolucion nula). Los préstamos devueltos no cuentan.
            prestamo.HasIndex(x => new { x.EquipoId, x.FechaDevolucion })
                .IsUnique().HasFilter("[FechaDevolucion] IS NULL");
            // Restrict: no se borra en cascada; SQL Server rechaza eliminar
            // un equipo o usuario que tenga préstamos.
            prestamo.HasOne(x => x.Equipo).WithMany(x => x.Prestamos)
                .HasForeignKey(x => x.EquipoId).OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne(x => x.Usuario).WithMany(x => x.Prestamos)
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
