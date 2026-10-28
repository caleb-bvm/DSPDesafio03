using System.ComponentModel.DataAnnotations;

namespace EquiposReportes.Models;

// Formulario de inicio de sesión.
public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre de usuario.")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}

// Respuesta de POST /api/auth/login
public class LoginRespuesta
{
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
