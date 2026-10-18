using EquiposAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Data;

public class EquiposDbContext(DbContextOptions<EquiposDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(categoria =>
        {
            categoria.ToTable("Categorias");
            categoria.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            categoria.HasIndex(x => x.Nombre).IsUnique();
            categoria.HasData(
                new Categoria { Id = 1, Nombre = "Laptops" },
                new Categoria { Id = 2, Nombre = "Proyectores" },
                new Categoria { Id = 3, Nombre = "Kits de IoT" },
                new Categoria { Id = 4, Nombre = "Tarjetas Arduino" });
        });

        modelBuilder.Entity<Equipo>(equipo =>
        {
            equipo.ToTable("Equipos", tabla =>
                tabla.HasCheckConstraint("CK_Equipos_Estado",
                    "[Estado] IN ('Disponible', 'Prestado', 'Mantenimiento')"));
            equipo.Property(x => x.CodigoInventario).HasMaxLength(50).IsRequired();
            equipo.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            equipo.Property(x => x.Descripcion).HasMaxLength(500);
            equipo.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            equipo.Property(x => x.RowVersion).IsRowVersion();
            equipo.HasIndex(x => x.CodigoInventario).IsUnique();
            equipo.HasOne(x => x.Categoria).WithMany(x => x.Equipos)
                .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });

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

        modelBuilder.Entity<Prestamo>(prestamo =>
        {
            prestamo.ToTable("Prestamos");
            prestamo.Property(x => x.Observaciones).HasMaxLength(500);
            prestamo.HasIndex(x => new { x.EquipoId, x.FechaDevolucion })
                .IsUnique().HasFilter("[FechaDevolucion] IS NULL");
            prestamo.HasOne(x => x.Equipo).WithMany(x => x.Prestamos)
                .HasForeignKey(x => x.EquipoId).OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne(x => x.Usuario).WithMany(x => x.Prestamos)
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
