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
    TokenService tokenService,
    IMapper mapper) : ControllerBase
{
    // POST /api/auth/login  (anónimo)
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginRespuestaDto>> Login(LoginDto datos)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Username == datos.Username);

        // Se compara la contraseña escrita con el hash guardado.
        if (usuario is null ||
            hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, datos.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        return Ok(tokenService.CrearToken(usuario));
    }

    // POST /api/auth/register  (solo Admin)
    [HttpPost("register")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UsuarioListaDto>> Registrar(RegistroUsuarioDto datos)
    {
        // Username repetido -> 409 Conflict.
        if (await db.Usuarios.AnyAsync(u => u.Username == datos.Username))
            return Conflict(new { message = "Ese nombre de usuario ya existe." });

        var usuario = new Usuario { Username = datos.Username, Email = datos.Email, Rol = datos.Rol };
        usuario.PasswordHash = hasher.HashPassword(usuario, datos.Password);   // Nunca texto plano.

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, mapper.Map<UsuarioListaDto>(usuario));
    }
}
