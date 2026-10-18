namespace EquiposAPI.Models;

public class Equipo
{
    public int Id { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public string Estado { get; set; } = EstadoEquipo.Disponible;
    public byte[] RowVersion { get; set; } = [];
    public List<Prestamo> Prestamos { get; set; } = [];
}
