namespace EquiposAPI.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = Roles.Estudiante;
    public List<Prestamo> Prestamos { get; set; } = [];
}
