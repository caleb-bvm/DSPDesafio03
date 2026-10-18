using System;
using System.Collections.Generic;
using EquiposReportes.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquiposReportes.Data;

public partial class EquiposDbContext : DbContext
{
    public EquiposDbContext(DbContextOptions<EquiposDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Equipo> Equipos { get; set; }

    public virtual DbSet<Prestamo> Prestamos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasIndex(e => e.Nombre, "IX_Categorias_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.HasIndex(e => e.CategoriaId, "IX_Equipos_CategoriaId");

            entity.HasIndex(e => e.CodigoInventario, "IX_Equipos_CodigoInventario").IsUnique();

            entity.Property(e => e.CodigoInventario).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(CONVERT([datetime2],((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central America Standard Time')))");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Categoria).WithMany(p => p.Equipos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.HasIndex(e => new { e.EquipoId, e.FechaDevolucion }, "IX_Prestamos_EquipoId_FechaDevolucion")
                .IsUnique()
                .HasFilter("([FechaDevolucion] IS NULL)");

            entity.HasIndex(e => e.UsuarioId, "IX_Prestamos_UsuarioId");

            entity.Property(e => e.Observaciones).HasMaxLength(500);

            entity.HasOne(d => d.Equipo).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.EquipoId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Username, "IX_Usuarios_Username").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Rol).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
