namespace EquiposAPI.Models;

public class Prestamo
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? Observaciones { get; set; }
}
