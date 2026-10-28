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
[Authorize]   // Todos los endpoints exigen token; los de escritura, además, rol Admin.
public class CategoriasController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    // GET /api/categorias
    [HttpGet]
    public async Task<ActionResult<List<CategoriaListaDto>>> Listar()
    {
        var categorias = await db.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        return Ok(mapper.Map<List<CategoriaListaDto>>(categorias));
    }

    // GET /api/categorias/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaListaDto>> Obtener(int id)
    {
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound(new { message = "Categoría no encontrada." });

        return Ok(mapper.Map<CategoriaListaDto>(categoria));
    }

    // POST /api/categorias
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoriaListaDto>> Crear(CategoriaCrearDto datos)
    {
        if (await db.Categorias.AnyAsync(c => c.Nombre == datos.Nombre))
            return Conflict(new { message = "Ya existe una categoría con ese nombre." });

        var categoria = mapper.Map<Categoria>(datos);
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { id = categoria.Id }, mapper.Map<CategoriaListaDto>(categoria));
    }

    // PUT /api/categorias/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoriaListaDto>> Actualizar(int id, CategoriaActualizarDto datos)
    {
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound(new { message = "Categoría no encontrada." });

        if (await db.Categorias.AnyAsync(c => c.Id != id && c.Nombre == datos.Nombre))
            return Conflict(new { message = "Ya existe una categoría con ese nombre." });

        mapper.Map(datos, categoria);   // Copia los datos del DTO a la entidad.
        await db.SaveChangesAsync();

        return Ok(mapper.Map<CategoriaListaDto>(categoria));
    }

    // DELETE /api/categorias/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound(new { message = "Categoría no encontrada." });

        // Restrict: una categoría con equipos no se puede borrar.
        if (await db.Equipos.AnyAsync(e => e.CategoriaId == id))
            return Conflict(new { message = "No se puede eliminar una categoría que tiene equipos." });

        db.Categorias.Remove(categoria);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
