using System.Security.Claims;
using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

[ApiController]
[Route("api/prestamos")]
[Authorize]
public class PrestamosController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    // GET /api/prestamos?soloVencidos=true
    // Admin y Docente ven todos. El Estudiante solo ve los suyos.
    [HttpGet]
    public async Task<ActionResult<List<PrestamoListaDto>>> Listar(bool soloVencidos = false)
    {
        var consulta = db.Prestamos.AsNoTracking()
            .Include(p => p.Equipo)
            .Include(p => p.Usuario)
            .AsQueryable();

        if (User.IsInRole(Roles.Estudiante))
        {
            // El Id se toma del token (claim NameIdentifier), NUNCA de un parámetro.
            // Así el estudiante no puede ver préstamos ajenos cambiando la URL.
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            consulta = consulta.Where(p => p.UsuarioId == usuarioId);
        }

        if (soloVencidos)
            consulta = consulta.Where(p => p.FechaDevolucion == null && p.FechaVencimiento < DateTime.Today);

        var prestamos = await consulta.OrderByDescending(p => p.FechaPrestamo).ToListAsync();
        return Ok(mapper.Map<List<PrestamoListaDto>>(prestamos));
    }

    // POST /api/prestamos  (Admin o Docente)
    [HttpPost]
    [Authorize(Roles = Roles.AdminODocente)]
    public async Task<ActionResult<PrestamoListaDto>> Crear(PrestamoCrearDto datos)
    {
        // Regla: el vencimiento va de hoy a hoy + 7 días.
        var hoy = DateTime.Today;
        var vencimiento = datos.FechaVencimiento.Date;
        if (vencimiento < hoy || vencimiento > hoy.AddDays(7))
            return BadRequest(new { message = "La fecha de vencimiento debe estar entre hoy y los próximos 7 días." });

        var equipo = await db.Equipos.FindAsync(datos.EquipoId);
        if (equipo is null)
            return NotFound(new { message = "Equipo no encontrado." });

        // Regla: solo se presta un equipo Disponible. Si no, 409 Conflict.
        if (equipo.Estado != EstadoEquipo.Disponible)
            return Conflict(new { message = $"El equipo no está disponible (estado actual: {equipo.Estado})." });

        var usuario = await db.Usuarios.FindAsync(datos.UsuarioId);
        if (usuario is null)
            return NotFound(new { message = "Usuario no encontrado." });

        var prestamo = new Prestamo
        {
            Equipo = equipo,
            Usuario = usuario,
            FechaPrestamo = DateTime.Now,
            FechaVencimiento = vencimiento,
            Observaciones = datos.Observaciones
        };
        db.Prestamos.Add(prestamo);
        equipo.Estado = EstadoEquipo.Prestado;

        try
        {
            // UN SOLO SaveChanges: guarda el préstamo y el nuevo estado del equipo
            // en la misma transacción. O se guardan los dos, o ninguno.
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro usuario prestó o editó el mismo equipo al mismo tiempo (RowVersion distinta).
            return Conflict(new { message = "El equipo acaba de cambiar. Vuelva a consultar su disponibilidad." });
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // El índice único filtrado rechazó un segundo préstamo activo del mismo equipo.
            return Conflict(new { message = "El equipo ya tiene un préstamo activo." });
        }

        return StatusCode(StatusCodes.Status201Created, mapper.Map<PrestamoListaDto>(prestamo));
    }

    // PUT /api/prestamos/5/devolucion  (Admin o Docente)
    [HttpPut("{id:int}/devolucion")]
    [Authorize(Roles = Roles.AdminODocente)]
    public async Task<ActionResult<PrestamoListaDto>> Devolver(int id)
    {
        var prestamo = await db.Prestamos
            .Include(p => p.Equipo)
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (prestamo is null)
            return NotFound(new { message = "Préstamo no encontrado." });
        if (prestamo.FechaDevolucion is not null)
            return Conflict(new { message = "Este préstamo ya fue devuelto." });

        // Se registra la devolución y el equipo vuelve a estar Disponible.
        prestamo.FechaDevolucion = DateTime.Now;
        prestamo.Equipo.Estado = EstadoEquipo.Disponible;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro usuario editó el equipo mientras se registraba la devolución.
            return Conflict(new { message = "El equipo acaba de cambiar. Intente registrar la devolución de nuevo." });
        }

        return Ok(mapper.Map<PrestamoListaDto>(prestamo));
    }
}
