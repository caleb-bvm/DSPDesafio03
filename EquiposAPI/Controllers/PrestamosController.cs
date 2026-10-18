using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using EquiposAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

[ApiController]
[Route("api/prestamos")]
[Authorize]
public class PrestamosController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PrestamoListaDto>>> Listar(bool soloVencidos = false)
    {
        IQueryable<Prestamo> consulta = db.Prestamos.AsNoTracking()
            .Include(x => x.Equipo).Include(x => x.Usuario);

        if (User.IsInRole(Roles.Estudiante))
        {
            var idEnToken = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(idEnToken, out var usuarioId))
                return Unauthorized(new { message = "La sesión no contiene un usuario válido." });
            consulta = consulta.Where(x => x.UsuarioId == usuarioId);
        }
        if (soloVencidos)
        {
            var hoy = RelojLocal.Hoy();
            consulta = consulta.Where(x => x.FechaDevolucion == null && x.FechaVencimiento < hoy);
        }

        var prestamos = await consulta.OrderByDescending(x => x.FechaPrestamo)
            .ThenByDescending(x => x.Id).ToListAsync();
        return Ok(mapper.Map<List<PrestamoListaDto>>(prestamos));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Docente")]
    public async Task<ActionResult<PrestamoListaDto>> Crear(PrestamoCrearDto datos)
    {
        var ahora = RelojLocal.Ahora();
        var fechaVencimiento = RelojLocal.DiaEnGuatemala(datos.FechaVencimiento);
        if (fechaVencimiento < ahora.Date || fechaVencimiento > ahora.Date.AddDays(7))
        {
            return BadRequest(new { message = "El vencimiento debe estar entre hoy y los próximos 7 días." });
        }

        var equipo = await db.Equipos.FindAsync(datos.EquipoId);
        if (equipo is null)
            return NotFound(new { message = "Equipo no encontrado." });
        if (equipo.Estado != EstadoEquipo.Disponible)
            return Conflict(new { message = "El equipo no está disponible para préstamo." });

        var usuario = await db.Usuarios.FindAsync(datos.UsuarioId);
        if (usuario is null)
            return NotFound(new { message = "Usuario no encontrado." });

        var prestamo = new Prestamo
        {
            EquipoId = equipo.Id,
            Equipo = equipo,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            FechaPrestamo = ahora,
            FechaVencimiento = fechaVencimiento,
            Observaciones = datos.Observaciones?.Trim()
        };
        equipo.Estado = EstadoEquipo.Prestado;
        db.Prestamos.Add(prestamo);

        try
        {
            // EF guarda el préstamo y el nuevo estado del equipo en una transacción.
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "El equipo cambió. Vuelva a consultar su disponibilidad." });
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsDuplicado(error))
        {
            return Conflict(new { message = "El equipo ya tiene un préstamo activo." });
        }

        return StatusCode(StatusCodes.Status201Created, mapper.Map<PrestamoListaDto>(prestamo));
    }

    [HttpPut("{id:int}/devolucion")]
    [Authorize(Roles = "Admin,Docente")]
    public async Task<ActionResult<PrestamoListaDto>> Devolver(int id)
    {
        var prestamo = await db.Prestamos.Include(x => x.Equipo).Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (prestamo is null)
            return NotFound(new { message = "Préstamo no encontrado." });
        if (prestamo.FechaDevolucion.HasValue)
            return Conflict(new { message = "Este préstamo ya fue devuelto." });

        prestamo.FechaDevolucion = RelojLocal.Ahora();
        prestamo.Equipo.Estado = EstadoEquipo.Disponible;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "El préstamo cambió. Vuelva a cargar la lista." });
        }

        return Ok(mapper.Map<PrestamoListaDto>(prestamo));
    }
}
