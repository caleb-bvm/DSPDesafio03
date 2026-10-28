using System.ComponentModel.DataAnnotations;

namespace EquiposAPI.Dtos;

// Entrada de POST /api/prestamos (Admin o Docente)
public class PrestamoCrearDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un equipo.")]
    public int EquipoId { get; set; }

    // Persona que recibe el equipo (prestatario).
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un usuario.")]
    public int UsuarioId { get; set; }

    // Debe estar entre hoy y hoy + 7 días (se valida en el controlador).
    public DateTime FechaVencimiento { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

// Salida de los GET y del POST
public class PrestamoListaDto
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;
    public string EquipoNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? Observaciones { get; set; }
}
