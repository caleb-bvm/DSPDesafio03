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
[Authorize]
public class EquiposController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaDto<EquipoListaDto>>> Listar(
        string? texto, int? categoriaId, string? estado, int page = 1, int pageSize = 10)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest(new { message = "La página debe ser mayor que cero y el tamaño entre 1 y 100." });

        IQueryable<Equipo> consulta = db.Equipos.AsNoTracking().Include(x => x.Categoria);

        if (User.IsInRole(Roles.Estudiante))
        {
            // Un estudiante solo consulta disponibles, aunque cambie el filtro en la URL.
            consulta = consulta.Where(x => x.Estado == EstadoEquipo.Disponible);
        }
        else if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoNormalizado = EstadoEquipo.Normalizar(estado);
            if (estadoNormalizado is null)
                return BadRequest(new { message = "El estado no es válido." });
            consulta = consulta.Where(x => x.Estado == estadoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var busqueda = texto.Trim();
            consulta = consulta.Where(x => x.CodigoInventario.Contains(busqueda) ||
                x.Nombre.Contains(busqueda) ||
                (x.Descripcion != null && x.Descripcion.Contains(busqueda)));
        }
        if (categoriaId.HasValue)
            consulta = consulta.Where(x => x.CategoriaId == categoriaId.Value);

        var total = await consulta.CountAsync();
        var equipos = await consulta.OrderBy(x => x.Nombre).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PaginaDto<EquipoListaDto>
        {
            Items = mapper.Map<List<EquipoListaDto>>(equipos),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipoListaDto>> Obtener(int id)
    {
        var equipo = await db.Equipos.AsNoTracking().Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (equipo is null ||
            (User.IsInRole(Roles.Estudiante) && equipo.Estado != EstadoEquipo.Disponible))
            return NotFound(new { message = "Equipo no encontrado." });

        return Ok(mapper.Map<EquipoListaDto>(equipo));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EquipoListaDto>> Crear(EquipoCrearDto datos)
    {
        var codigo = datos.CodigoInventario.Trim();
        var nombre = datos.Nombre.Trim();
        var estado = EstadoEquipo.Normalizar(datos.Estado);
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { message = "El código y el nombre son obligatorios." });
        if (estado is null)
            return BadRequest(new { message = "El estado no es válido." });

        var categoria = await db.Categorias.FindAsync(datos.CategoriaId);
        if (categoria is null)
            return BadRequest(new { message = "Seleccione una categoría existente." });
        if (await db.Equipos.AnyAsync(x => x.CodigoInventario == codigo))
            return Conflict(new { message = "Ya existe un equipo con ese código de inventario." });

        var equipo = mapper.Map<Equipo>(datos);
        equipo.CodigoInventario = codigo;
        equipo.Nombre = nombre;
        equipo.Estado = estado;
        equipo.Categoria = categoria;
        equipo.Descripcion = datos.Descripcion?.Trim();
        db.Equipos.Add(equipo);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsDuplicado(error))
        {
            return Conflict(new { message = "Ya existe un equipo con ese código de inventario." });
        }

        return CreatedAtAction(nameof(Obtener), new { id = equipo.Id },
            mapper.Map<EquipoListaDto>(equipo));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EquipoListaDto>> Actualizar(int id, EquipoActualizarDto datos)
    {
        byte[] version;
        try
        {
            version = Convert.FromBase64String(datos.RowVersion);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "La versión del equipo no es válida." });
        }
        if (version.Length != 8)
            return BadRequest(new { message = "La versión del equipo no es válida." });

        var equipo = await db.Equipos.Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (equipo is null)
            return NotFound(new { message = "Equipo no encontrado." });
        if (!equipo.RowVersion.SequenceEqual(version))
            return Conflict(new { message = "El equipo fue modificado por otra persona. Vuelva a cargarlo." });

        var codigo = datos.CodigoInventario.Trim();
        var nombre = datos.Nombre.Trim();
        var estado = EstadoEquipo.Normalizar(datos.Estado);
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { message = "El código y el nombre son obligatorios." });
        if (estado is null)
            return BadRequest(new { message = "El estado no es válido." });

        var categoria = await db.Categorias.FindAsync(datos.CategoriaId);
        if (categoria is null)
            return BadRequest(new { message = "Seleccione una categoría existente." });
        if (await db.Equipos.AnyAsync(x => x.Id != id && x.CodigoInventario == codigo))
            return Conflict(new { message = "Ya existe un equipo con ese código de inventario." });
        if (estado != EstadoEquipo.Prestado &&
            await db.Prestamos.AnyAsync(x => x.EquipoId == id && x.FechaDevolucion == null))
        {
            return Conflict(new { message = "El equipo tiene un préstamo activo y debe seguir Prestado." });
        }

        db.Entry(equipo).Property(x => x.RowVersion).OriginalValue = version;
        mapper.Map(datos, equipo);
        equipo.CodigoInventario = codigo;
        equipo.Nombre = nombre;
        equipo.Estado = estado;
        equipo.Categoria = categoria;
        equipo.Descripcion = datos.Descripcion?.Trim();
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "El equipo fue modificado por otra persona. Vuelva a cargarlo." });
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsDuplicado(error))
        {
            return Conflict(new { message = "Ya existe un equipo con ese código de inventario." });
        }

        return Ok(mapper.Map<EquipoListaDto>(equipo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var equipo = await db.Equipos.FindAsync(id);
        if (equipo is null)
            return NotFound(new { message = "Equipo no encontrado." });
        if (await db.Prestamos.AnyAsync(x => x.EquipoId == id))
            return Conflict(new { message = "No se puede eliminar un equipo con préstamos históricos." });

        db.Equipos.Remove(equipo);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsRestriccion(error))
        {
            return Conflict(new { message = "No se puede eliminar un equipo con préstamos históricos." });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "El equipo cambió mientras se intentaba eliminar. Vuelva a cargarlo." });
        }

        return NoContent();
    }
}
