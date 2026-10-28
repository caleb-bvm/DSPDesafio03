using System.ComponentModel.DataAnnotations;

namespace EquiposAPI.Dtos;

// Entrada de POST /api/categorias
public class CategoriaCrearDto
{
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
}

// Entrada de PUT /api/categorias/{id}
public class CategoriaActualizarDto
{
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
}

// Salida de los GET
public class CategoriaListaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
