using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

// Lista de usuarios para elegir al prestatario en el formulario de préstamo.
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Roles.AdminODocente)]
public class UsuariosController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    // GET /api/usuarios
    [HttpGet]
    public async Task<ActionResult<List<UsuarioListaDto>>> Listar()
    {
        var usuarios = await db.Usuarios.AsNoTracking().OrderBy(u => u.Username).ToListAsync();
        return Ok(mapper.Map<List<UsuarioListaDto>>(usuarios));
    }
}
