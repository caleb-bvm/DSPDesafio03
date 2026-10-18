using System.ComponentModel.DataAnnotations;

namespace EquiposAPI.Dtos;

public class PrestamoCrearDto
{
    [Range(1, int.MaxValue)]
    public int EquipoId { get; set; }

    [Range(1, int.MaxValue)]
    public int UsuarioId { get; set; }

    public DateTime FechaVencimiento { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

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
