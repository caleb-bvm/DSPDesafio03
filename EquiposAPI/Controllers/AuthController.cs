using AutoMapper;
using EquiposAPI.Data;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using EquiposAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    EquiposDbContext db,
    IPasswordHasher<Usuario> hasher,
    TokenService tokens,
    IMapper mapper) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginRespuestaDto>> Login(LoginDto datos)
    {
        var username = datos.Username.Trim();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.Username == username);
        if (usuario is null ||
            hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, datos.Password)
                == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        return Ok(tokens.CrearToken(usuario));
    }

    [HttpPost("register")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UsuarioListaDto>> Registrar(RegistroUsuarioDto datos)
    {
        var username = datos.Username.Trim();
        var email = datos.Email.Trim();
        var rol = Roles.Normalizar(datos.Rol);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "El usuario y correo son obligatorios." });
        if (rol is null)
            return BadRequest(new { message = "El rol debe ser Admin, Docente o Estudiante." });
        if (await db.Usuarios.AnyAsync(x => x.Username == username))
            return Conflict(new { message = "Ese nombre de usuario ya existe." });

        var usuario = new Usuario { Username = username, Email = email, Rol = rol };
        usuario.PasswordHash = hasher.HashPassword(usuario, datos.Password);
        db.Usuarios.Add(usuario);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresBaseDatos.EsDuplicado(error))
        {
            return Conflict(new { message = "Ese nombre de usuario ya existe." });
        }

        return StatusCode(StatusCodes.Status201Created, mapper.Map<UsuarioListaDto>(usuario));
    }
}
