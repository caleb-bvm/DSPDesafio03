using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace EquiposAPI.Services;

// Crea el token JWT que se entrega al iniciar sesión.
public class TokenService(IConfiguration config)
{
    public LoginRespuestaDto CrearToken(Usuario usuario)
    {
        var vence = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:MinutesToExpire"));

        // Claims: datos del usuario que viajan dentro del token ("nameid", "unique_name", "role").
        // Al recibir el token, ASP.NET los convierte a ClaimTypes.NameIdentifier,
        // ClaimTypes.Name y ClaimTypes.Role; por eso funcionan User.FindFirstValue(...)
        // y [Authorize(Roles = ...)].
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.NameId, usuario.Id.ToString()),   // Id del usuario
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Username),
            new Claim("role", usuario.Rol)                                      // Rol
        };

        // Firma con la clave secreta (mínimo 32 caracteres, guardada en User Secrets).
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var firma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: vence,
            signingCredentials: firma);

        return new LoginRespuestaDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = usuario.Username,
            Rol = usuario.Rol,
            ExpiresAt = vence
        };
    }
}
