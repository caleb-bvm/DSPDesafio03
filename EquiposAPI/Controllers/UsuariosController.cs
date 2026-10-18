using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Admin,Docente")]
public class UsuariosController(EquiposDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UsuarioListaDto>>> Listar()
    {
        var usuarios = await db.Usuarios.AsNoTracking()
            .OrderBy(x => x.Username).ToListAsync();
        return Ok(mapper.Map<List<UsuarioListaDto>>(usuarios));
    }
}
