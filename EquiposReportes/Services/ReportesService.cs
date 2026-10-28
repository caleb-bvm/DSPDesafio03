using EquiposReportes.Data;
using EquiposReportes.Models;
using Microsoft.EntityFrameworkCore;

namespace EquiposReportes.Services;

// Consultas LINQ de los cuatro reportes. SOLO LECTURA:
//   - Todas usan AsNoTracking() (EF no rastrea cambios).
//   - Todas proyectan con Select a un ViewModel (no se devuelven entidades).
//   - La conexión usa el usuario SQL "reportes", que solo tiene db_datareader.
//
// Filtro de fechas: "desde" incluye todo ese día y "hasta" también, por eso
// se compara con "< hasta + 1 día".
public class ReportesService(EquiposDbContext db)
{
    // Opciones para el filtro de categoría.
    public Task<List<CategoriaOpcionViewModel>> CategoriasAsync() =>
        db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaOpcionViewModel { Id = c.Id, Nombre = c.Nombre })
            .ToListAsync();

    // 1. Cantidad de equipos por categoría y por estado.
    //    Las fechas filtran por FechaRegistro (fecha de alta del equipo).
    public Task<List<InventarioFilaViewModel>> InventarioAsync(FiltrosViewModel f)
    {
        var equipos = db.Equipos.AsNoTracking();
        if (f.Desde.HasValue)
            equipos = equipos.Where(e => e.FechaRegistro >= f.Desde.Value);
        if (f.Hasta.HasValue)
        {
            var hastaFin = f.Hasta.Value.AddDays(1);
            equipos = equipos.Where(e => e.FechaRegistro < hastaFin);
        }

        var categorias = db.Categorias.AsNoTracking();
        if (f.CategoriaId.HasValue)
            categorias = categorias.Where(c => c.Id == f.CategoriaId);

        // Una fila por categoría (aunque tenga 0 equipos) con un conteo por estado.
        return categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new InventarioFilaViewModel
            {
                Categoria = c.Nombre,
                Disponibles = equipos.Count(e => e.CategoriaId == c.Id && e.Estado == "Disponible"),
                Prestados = equipos.Count(e => e.CategoriaId == c.Id && e.Estado == "Prestado"),
                EnMantenimiento = equipos.Count(e => e.CategoriaId == c.Id && e.Estado == "Mantenimiento")
            })
            .ToListAsync();
    }

    // 2. Préstamos vencidos: FechaVencimiento < hoy y FechaDevolucion nula.
    //    Las fechas filtran por FechaVencimiento.
    public Task<List<VencidoFilaViewModel>> VencidosAsync(FiltrosViewModel f)
    {
        var hoy = DateTime.Today;
        var prestamos = db.Prestamos.AsNoTracking()
            .Where(p => p.FechaDevolucion == null && p.FechaVencimiento < hoy);

        if (f.CategoriaId.HasValue)
            prestamos = prestamos.Where(p => p.Equipo.CategoriaId == f.CategoriaId);
        if (f.Desde.HasValue)
            prestamos = prestamos.Where(p => p.FechaVencimiento >= f.Desde.Value);
        if (f.Hasta.HasValue)
        {
            var hastaFin = f.Hasta.Value.AddDays(1);
            prestamos = prestamos.Where(p => p.FechaVencimiento < hastaFin);
        }

        return prestamos
            .OrderBy(p => p.FechaVencimiento)
            .Select(p => new VencidoFilaViewModel
            {
                Username = p.Usuario.Username,
                CodigoInventario = p.Equipo.CodigoInventario,
                Equipo = p.Equipo.Nombre,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                // Se traduce a DATEDIFF(day, FechaVencimiento, hoy) en SQL Server.
                DiasAtraso = EF.Functions.DateDiffDay(p.FechaVencimiento, hoy)
            })
            .ToListAsync();
    }

    // 3. Los 5 equipos con más préstamos. Las fechas filtran por FechaPrestamo.
    public Task<List<TopEquipoFilaViewModel>> TopEquiposAsync(FiltrosViewModel f)
    {
        var prestamos = db.Prestamos.AsNoTracking();

        if (f.CategoriaId.HasValue)
            prestamos = prestamos.Where(p => p.Equipo.CategoriaId == f.CategoriaId);
        if (f.Desde.HasValue)
            prestamos = prestamos.Where(p => p.FechaPrestamo >= f.Desde.Value);
        if (f.Hasta.HasValue)
        {
            var hastaFin = f.Hasta.Value.AddDays(1);
            prestamos = prestamos.Where(p => p.FechaPrestamo < hastaFin);
        }

        // Se agrupan los préstamos por equipo y se cuentan.
        return prestamos
            .GroupBy(p => new
            {
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
            .OrderByDescending(fila => fila.CantidadPrestamos)
            .ThenBy(fila => fila.CodigoInventario)
            .Take(5)
            .ToListAsync();
    }

    // 4. Todos los préstamos de un usuario (búsqueda por Username).
    //    Las fechas filtran por FechaPrestamo.
    public Task<List<HistorialFilaViewModel>> HistorialAsync(FiltrosViewModel f)
    {
        var prestamos = db.Prestamos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(f.Username))
            prestamos = prestamos.Where(p => p.Usuario.Username.Contains(f.Username));
        if (f.CategoriaId.HasValue)
            prestamos = prestamos.Where(p => p.Equipo.CategoriaId == f.CategoriaId);
        if (f.Desde.HasValue)
            prestamos = prestamos.Where(p => p.FechaPrestamo >= f.Desde.Value);
        if (f.Hasta.HasValue)
        {
            var hastaFin = f.Hasta.Value.AddDays(1);
            prestamos = prestamos.Where(p => p.FechaPrestamo < hastaFin);
        }

        return prestamos
            .OrderByDescending(p => p.FechaPrestamo)
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
