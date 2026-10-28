namespace EquiposAPI.Models;

// Registro de un préstamo. Un equipo y un usuario pueden tener muchos préstamos.
public class Prestamo
{
    public int Id { get; set; }

    public int EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }

    // Queda en null mientras el equipo no se devuelva.
    public DateTime? FechaDevolucion { get; set; }

    public string? Observaciones { get; set; }
}
