using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EquiposAPI.Dtos;
using EquiposAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace EquiposAPI.Services;

public class TokenService(IConfiguration configuracion)
{
    public LoginRespuestaDto CrearToken(Usuario usuario)
    {
        var ahora = DateTime.UtcNow;
        var minutos = configuracion.GetValue<int>("Jwt:MinutesToExpire");
        var vence = ahora.AddMinutes(minutos);
        var clave = configuracion["Jwt:Key"]!;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Username),
            new Claim("role", usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: configuracion["Jwt:Issuer"],
            audience: configuracion["Jwt:Audience"],
            claims: claims,
            notBefore: ahora,
            expires: vence,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256));

        return new LoginRespuestaDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = usuario.Username,
            Rol = usuario.Rol,
            ExpiresAt = vence
        };
    }
}
