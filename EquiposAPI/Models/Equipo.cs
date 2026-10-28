namespace EquiposAPI.Models;

// Equipo del laboratorio. Relación: Equipo 1-N Prestamo.
public class Equipo
{
    public int Id { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;   // Único.
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    // Disponible, Prestado o Mantenimiento (ver EstadoEquipo).
    public string Estado { get; set; } = EstadoEquipo.Disponible;

    // Fecha de alta. Se usa como filtro de fechas en el reporte de inventario.
    public DateTime FechaRegistro { get; set; }

    // SQL Server cambia este valor en cada UPDATE. Sirve para la concurrencia
    // optimista: si cambió desde que se leyó, la actualización falla.
    public byte[] RowVersion { get; set; } = [];

    public List<Prestamo> Prestamos { get; set; } = [];
}
