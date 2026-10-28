using System.ComponentModel.DataAnnotations;
using EquiposAPI.Models;

namespace EquiposAPI.Dtos;

// Entrada de POST /api/equipos
public class EquipoCrearDto
{
    [Required(ErrorMessage = "El código de inventario es obligatorio."), StringLength(50)]
    public string CodigoInventario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaId { get; set; }

    [Required]
    [AllowedValues(EstadoEquipo.Disponible, EstadoEquipo.Prestado, EstadoEquipo.Mantenimiento,
        ErrorMessage = "El estado debe ser Disponible, Prestado o Mantenimiento.")]
    public string Estado { get; set; } = string.Empty;
}

// Entrada de PUT /api/equipos/{id}. Igual que crear, pero además trae la
// RowVersion (en Base64) que el cliente recibió al abrir la edición.
public class EquipoActualizarDto : EquipoCrearDto
{
    [Required(ErrorMessage = "Falta la versión del equipo (RowVersion).")]
    public string RowVersion { get; set; } = string.Empty;
}

// Salida de los GET. Incluye el nombre de la categoría y la RowVersion en Base64.
public class EquipoListaDto
{
    public int Id { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

// Resultado paginado de GET /api/equipos
public class PaginaDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
