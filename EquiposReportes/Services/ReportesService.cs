using EquiposReportes.Data;
using EquiposReportes.Models;
using Microsoft.EntityFrameworkCore;

namespace EquiposReportes.Services;

// Este servicio solo consulta. La conexión debe usar un usuario SQL con db_datareader.
public class ReportesService(EquiposDbContext db)
{
    private static readonly TimeZoneInfo ZonaLaboratorio =
        TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");

    public Task<List<CategoriaOpcionViewModel>> CategoriasAsync() =>
        db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaOpcionViewModel { Id = c.Id, Nombre = c.Nombre })
            .ToListAsync();

    public Task<List<InventarioFilaViewModel>> InventarioAsync(
        DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var categorias = db.Categorias.AsNoTracking().AsQueryable();
        if (categoriaId.HasValue)
            categorias = categorias.Where(c => c.Id == categoriaId.Value);

        var inicio = desde?.ToDateTime(TimeOnly.MinValue);
        var fin = hasta?.ToDateTime(TimeOnly.MaxValue);

        return categorias.OrderBy(c => c.Nombre)
            .Select(c => new InventarioFilaViewModel
            {
                Categoria = c.Nombre,
                Disponibles = c.Equipos.Count(e => e.Estado == "Disponible"
                    && (!inicio.HasValue || e.FechaRegistro >= inicio.Value)
                    && (!fin.HasValue || e.FechaRegistro <= fin.Value)),
                Prestados = c.Equipos.Count(e => e.Estado == "Prestado"
                    && (!inicio.HasValue || e.FechaRegistro >= inicio.Value)
                    && (!fin.HasValue || e.FechaRegistro <= fin.Value)),
                EnMantenimiento = c.Equipos.Count(e => e.Estado == "Mantenimiento"
                    && (!inicio.HasValue || e.FechaRegistro >= inicio.Value)
                    && (!fin.HasValue || e.FechaRegistro <= fin.Value))
            })
            .ToListAsync();
    }

    public Task<List<VencidoFilaViewModel>> VencidosAsync(
        DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var hoy = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaLaboratorio).Date;
        var prestamos = db.Prestamos.AsNoTracking()
            .Where(p => p.FechaDevolucion == null && p.FechaVencimiento < hoy);

        if (categoriaId.HasValue)
            prestamos = prestamos.Where(p => p.Equipo.CategoriaId == categoriaId.Value);
        if (desde.HasValue)
            prestamos = prestamos.Where(p => p.FechaVencimiento >= desde.Value.ToDateTime(TimeOnly.MinValue));
        if (hasta.HasValue)
            prestamos = prestamos.Where(p => p.FechaVencimiento <= hasta.Value.ToDateTime(TimeOnly.MaxValue));

        return prestamos.OrderBy(p => p.FechaVencimiento)
            .Select(p => new VencidoFilaViewModel
            {
                Username = p.Usuario.Username,
                CodigoInventario = p.Equipo.CodigoInventario,
                Equipo = p.Equipo.Nombre,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                DiasAtraso = EF.Functions.DateDiffDay(p.FechaVencimiento, hoy)
            })
            .ToListAsync();
    }

    public Task<List<TopEquipoFilaViewModel>> TopEquiposAsync(
        DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var prestamos = db.Prestamos.AsNoTracking().AsQueryable();
        if (categoriaId.HasValue)
            prestamos = prestamos.Where(p => p.Equipo.CategoriaId == categoriaId.Value);
        if (desde.HasValue)
            prestamos = prestamos.Where(p => p.FechaPrestamo >= desde.Value.ToDateTime(TimeOnly.MinValue));
        if (hasta.HasValue)
            prestamos = prestamos.Where(p => p.FechaPrestamo <= hasta.Value.ToDateTime(TimeOnly.MaxValue));

        return prestamos
            .GroupBy(p => new
            {
                p.EquipoId,
                p.Equipo.CodigoInventario,
                p.Equipo.Nombre,
                Categoria = p.Equipo.Categoria.Nombre
            })
            .Select(g => new TopEquipoFilaViewModel
            {
                CodigoInventario = g.Key.CodigoInventario,
                Equipo = g.Key.Nombre,
                Categoria = g.Key.Categoria,
                CantidadPrestamos = g.Count()
            })
            .OrderByDescending(f => f.CantidadPrestamos)
            .ThenBy(f => f.CodigoInventario)
            .Take(5)
            .ToListAsync();
    }

    public Task<List<HistorialFilaViewModel>> HistorialAsync(
        string? username, DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var prestamos = db.Prestamos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(username))
        {
            var texto = username.Trim();
            prestamos = prestamos.Where(p => p.Usuario.Username.Contains(texto));
        }
        if (categoriaId.HasValue)
            prestamos = prestamos.Where(p => p.Equipo.CategoriaId == categoriaId.Value);
        if (desde.HasValue)
            prestamos = prestamos.Where(p => p.FechaPrestamo >= desde.Value.ToDateTime(TimeOnly.MinValue));
        if (hasta.HasValue)
            prestamos = prestamos.Where(p => p.FechaPrestamo <= hasta.Value.ToDateTime(TimeOnly.MaxValue));

        return prestamos.OrderByDescending(p => p.FechaPrestamo)
            .Select(p => new HistorialFilaViewModel
            {
                Username = p.Usuario.Username,
                CodigoInventario = p.Equipo.CodigoInventario,
                Equipo = p.Equipo.Nombre,
                Categoria = p.Equipo.Categoria.Nombre,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                FechaDevolucion = p.FechaDevolucion
            })
            .ToListAsync();
    }
}
