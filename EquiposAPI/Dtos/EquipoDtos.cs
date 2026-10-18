using System.ComponentModel.DataAnnotations;

namespace EquiposAPI.Dtos;

public class EquipoCrearDto
{
    [Required, StringLength(50)]
    public string CodigoInventario { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; set; }

    [Required]
    public string Estado { get; set; } = string.Empty;
}

public class EquipoActualizarDto : EquipoCrearDto
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public class EquipoListaDto
{
    public int Id { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public class PaginaDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
