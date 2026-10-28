using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

[ApiController]
[Route("api/equipos")]
[Authorize]   // Todos los endpoints exigen token; los de escritura, además, rol Admin.
public class EquiposController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    // GET /api/equipos?texto=&categoriaId=&estado=&page=1&pageSize=10
    // Los filtros son opcionales y se pueden combinar.
    [HttpGet]
    public async Task<ActionResult<PaginaDto<EquipoListaDto>>> Listar(
        string? texto, int? categoriaId, string? estado, int page = 1, int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "page debe ser mayor que 0 y pageSize estar entre 1 y 100." });

        // La consulta se arma por partes y se ejecuta al final (CountAsync / ToListAsync).
        var consulta = db.Equipos.AsNoTracking().Include(e => e.Categoria).AsQueryable();

        // Un Estudiante solo puede ver equipos disponibles, aunque cambie el filtro en la URL.
        if (User.IsInRole(Roles.Estudiante))
            estado = EstadoEquipo.Disponible;

        if (!string.IsNullOrWhiteSpace(texto))
            consulta = consulta.Where(e => e.Nombre.Contains(texto) || e.CodigoInventario.Contains(texto));
        if (categoriaId.HasValue)
            consulta = consulta.Where(e => e.CategoriaId == categoriaId);
        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(e => e.Estado == estado);

        var total = await consulta.CountAsync();
        var equipos = await consulta
            .OrderBy(e => e.Nombre)
            .Skip((page - 1) * pageSize)   // Salta las páginas anteriores.
            .Take(pageSize)                // Toma solo los de esta página.
            .ToListAsync();

        return Ok(new PaginaDto<EquipoListaDto>
        {
            Items = mapper.Map<List<EquipoListaDto>>(equipos),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    // GET /api/equipos/5  (detalle con categoría)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipoListaDto>> Obtener(int id)
    {
        var equipo = await db.Equipos.AsNoTracking().Include(e => e.Categoria)
            .FirstOrDefaultAsync(e => e.Id == id);
        // Igual que en el listado: el Estudiante solo puede ver equipos disponibles.
        if (equipo is null || (User.IsInRole(Roles.Estudiante) && equipo.Estado != EstadoEquipo.Disponible))
            return NotFound(new { message = "Equipo no encontrado." });

        return Ok(mapper.Map<EquipoListaDto>(equipo));
    }

    // POST /api/equipos
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EquipoListaDto>> Crear(EquipoCrearDto datos)
    {
        var categoria = await db.Categorias.FindAsync(datos.CategoriaId);
        if (categoria is null)
            return BadRequest(new { message = "La categoría no existe." });

        if (await db.Equipos.AnyAsync(e => e.CodigoInventario == datos.CodigoInventario))
            return Conflict(new { message = "Ya existe un equipo con ese código de inventario." });

        // Un equipo nuevo no tiene préstamos: solo pasa a Prestado al registrar uno.
        if (datos.Estado == EstadoEquipo.Prestado)
            return BadRequest(new { message = "Un equipo nuevo no puede estar Prestado; registre un préstamo." });

        var equipo = mapper.Map<Equipo>(datos);
        equipo.Categoria = categoria;
        equipo.FechaRegistro = DateTime.Now;

        db.Equipos.Add(equipo);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { id = equipo.Id }, mapper.Map<EquipoListaDto>(equipo));
    }

    // PUT /api/equipos/5  (usa concurrencia optimista con RowVersion)
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EquipoListaDto>> Actualizar(int id, EquipoActualizarDto datos)
    {
        var equipo = await db.Equipos.Include(e => e.Categoria).FirstOrDefaultAsync(e => e.Id == id);
        if (equipo is null)
            return NotFound(new { message = "Equipo no encontrado." });

        var categoria = await db.Categorias.FindAsync(datos.CategoriaId);
        if (categoria is null)
            return BadRequest(new { message = "La categoría no existe." });

        if (await db.Equipos.AnyAsync(e => e.Id != id && e.CodigoInventario == datos.CodigoInventario))
            return Conflict(new { message = "Ya existe un equipo con ese código de inventario." });

        // Si el equipo está prestado, no se le puede cambiar el estado a mano:
        // primero hay que registrar la devolución.
        var tienePrestamoActivo = await db.Prestamos.AnyAsync(p => p.EquipoId == id && p.FechaDevolucion == null);
        if (tienePrestamoActivo && datos.Estado != EstadoEquipo.Prestado)
            return Conflict(new { message = "El equipo tiene un préstamo activo; registre primero la devolución." });
        // Y al revés: sin préstamo activo no se puede marcar como Prestado,
        // porque quedaría un equipo que nadie puede devolver.
        if (!tienePrestamoActivo && datos.Estado == EstadoEquipo.Prestado)
            return BadRequest(new { message = "Para marcar el equipo como Prestado, registre un préstamo." });

        // CONCURRENCIA: le decimos a EF que la versión "original" es la que el
        // cliente leyó. EF genera: UPDATE ... WHERE Id = @id AND RowVersion = @versionLeida
        // Si otra persona guardó antes, la RowVersion ya cambió, no se actualiza
        // ninguna fila y EF lanza DbUpdateConcurrencyException.
        try
        {
            db.Entry(equipo).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(datos.RowVersion);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "La RowVersion enviada no es válida." });
        }

        mapper.Map(datos, equipo);
        equipo.Categoria = categoria;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Otro usuario modificó este equipo. Vuelva a cargarlo antes de guardar." });
        }

        return Ok(mapper.Map<EquipoListaDto>(equipo));
    }

    // DELETE /api/equipos/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var equipo = await db.Equipos.FindAsync(id);
        if (equipo is null)
            return NotFound(new { message = "Equipo no encontrado." });

        // Regla de negocio: un equipo con préstamos históricos no se elimina.
        if (await db.Prestamos.AnyAsync(p => p.EquipoId == id))
            return Conflict(new { message = "No se puede eliminar un equipo con préstamos históricos." });

        db.Equipos.Remove(equipo);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
