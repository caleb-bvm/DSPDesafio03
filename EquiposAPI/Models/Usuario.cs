namespace EquiposAPI.Models;

// Usuario del sistema. Relación: Usuario 1-N Prestamo.
public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;       // Único.

    // Nunca se guarda la contraseña: solo su hash (PasswordHasher<Usuario>).
    public string PasswordHash { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = Roles.Estudiante;        // Admin, Docente o Estudiante.

    public List<Prestamo> Prestamos { get; set; } = [];
}
