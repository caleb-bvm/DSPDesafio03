using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoriaListaDto>>> Listar()
    {
        var categorias = await db.Categorias.AsNoTracking()
            .OrderBy(x => x.Nombre).ToListAsync();
        return Ok(mapper.Map<List<CategoriaListaDto>>(categorias));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaListaDto>> Obtener(int id)
    {
        var categoria = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return categoria is null ? NotFound(new { message = "Categoría no encontrada." })
            : Ok(mapper.Map<CategoriaListaDto>(categoria));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoriaListaDto>> Crear(CategoriaCrearDto datos)
    {
        var nombre = datos.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { message = "El nombre es obligatorio." });
        if (await db.Categorias.AnyAsync(x => x.Nombre == nombre))
            return Conflict(new { message = "Ya existe una categoría con ese nombre." });

        var categoria = mapper.Map<Categoria>(datos);
        categoria.Nombre = nombre;
        db.Categorias.Add(categoria);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsDuplicado(error))
        {
            return Conflict(new { message = "Ya existe una categoría con ese nombre." });
        }

        return CreatedAtAction(nameof(Obtener), new { id = categoria.Id },
            mapper.Map<CategoriaListaDto>(categoria));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoriaListaDto>> Actualizar(int id, CategoriaActualizarDto datos)
    {
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound(new { message = "Categoría no encontrada." });

        var nombre = datos.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { message = "El nombre es obligatorio." });
        if (await db.Categorias.AnyAsync(x => x.Id != id && x.Nombre == nombre))
            return Conflict(new { message = "Ya existe una categoría con ese nombre." });

        mapper.Map(datos, categoria);
        categoria.Nombre = nombre;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsDuplicado(error))
        {
            return Conflict(new { message = "Ya existe una categoría con ese nombre." });
        }

        return Ok(mapper.Map<CategoriaListaDto>(categoria));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound(new { message = "Categoría no encontrada." });
        if (await db.Equipos.AnyAsync(x => x.CategoriaId == id))
            return Conflict(new { message = "No se puede eliminar una categoría que tiene equipos." });

        db.Categorias.Remove(categoria);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsRestriccion(error))
        {
            return Conflict(new { message = "No se puede eliminar una categoría que tiene equipos." });
        }

        return NoContent();
    }
}
