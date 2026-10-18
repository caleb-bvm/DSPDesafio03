using System.ComponentModel.DataAnnotations;

namespace EquiposAPI.Dtos;

public class CategoriaCrearDto
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
}

public class CategoriaActualizarDto
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
}

public class CategoriaListaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
