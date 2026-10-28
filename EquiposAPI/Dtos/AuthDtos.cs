using System.ComponentModel.DataAnnotations;
using EquiposAPI.Models;

namespace EquiposAPI.Dtos;

// Entrada de POST /api/auth/login
public class LoginDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

// Salida de POST /api/auth/login
public class LoginRespuestaDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }   // Hora UTC en que vence el token.
}

// Entrada de POST /api/auth/register (solo Admin)
public class RegistroUsuarioDto
{
    [Required(ErrorMessage = "El usuario es obligatorio."), StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [AllowedValues(Roles.Admin, Roles.Docente, Roles.Estudiante,
        ErrorMessage = "El rol debe ser Admin, Docente o Estudiante.")]
    public string Rol { get; set; } = string.Empty;
}

// Salida para listar usuarios. No incluye PasswordHash.
public class UsuarioListaDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
