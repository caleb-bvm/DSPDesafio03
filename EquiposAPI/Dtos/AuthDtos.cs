using System.ComponentModel.DataAnnotations;

namespace EquiposAPI.Dtos;

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegistroUsuarioDto
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = string.Empty;
}

public class UsuarioListaDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public class LoginRespuestaDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
