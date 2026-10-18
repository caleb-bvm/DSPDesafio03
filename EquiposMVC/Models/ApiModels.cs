using System.ComponentModel.DataAnnotations;

namespace EquiposMVC.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Escribe tu nombre de usuario.")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string Rol { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
}

public class RegistroForm
{
    [Required(ErrorMessage = "Escribe un nombre de usuario.")]
    [StringLength(100, ErrorMessage = "Usa hasta 100 caracteres.")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Escribe un correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(256, ErrorMessage = "Usa hasta 256 caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [MinLength(8, ErrorMessage = "Usa al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Selecciona un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Estudiante";
}

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class CategoriaForm
{
    [Required(ErrorMessage = "Escribe un nombre.")]
    [StringLength(100, ErrorMessage = "Usa hasta 100 caracteres.")]
    [Display(Name = "Nombre de la categoría")]
    public string Nombre { get; set; } = "";
}

public class EquipoDto
{
    public int Id { get; set; }
    public string CodigoInventario { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = "";
    public string Estado { get; set; } = "";
    public string RowVersion { get; set; } = "";
}

public class EquipoForm
{
    [Required(ErrorMessage = "Escribe el código de inventario.")]
    [StringLength(50, ErrorMessage = "Usa hasta 50 caracteres.")]
    [Display(Name = "Código de inventario")]
    public string CodigoInventario { get; set; } = "";

    [Required(ErrorMessage = "Escribe el nombre del equipo.")]
    [StringLength(150, ErrorMessage = "Usa hasta 150 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Descripción")]
    [StringLength(500, ErrorMessage = "Usa hasta 500 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Disponible";

    // La versión leída al abrir Edit debe regresar intacta a la API.
    public string RowVersion { get; set; } = "";
}

public class PaginaDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class PrestamoDto
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public string CodigoInventario { get; set; } = "";
    public string EquipoNombre { get; set; } = "";
    public int UsuarioId { get; set; }
    public string Username { get; set; } = "";
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? Observaciones { get; set; }
}

public class PrestamoForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo.")]
    [Display(Name = "Equipo disponible")]
    public int EquipoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un usuario.")]
    [Display(Name = "Prestatario")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "Indica la fecha de vencimiento.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de vencimiento")]
    public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Observaciones")]
    [StringLength(500, ErrorMessage = "Usa hasta 500 caracteres.")]
    public string? Observaciones { get; set; }
}

public class EquipoIndexViewModel
{
    public PaginaDto<EquipoDto> Pagina { get; set; } = new();
    public List<CategoriaDto> Categorias { get; set; } = [];
    public string? Texto { get; set; }
    public int? CategoriaId { get; set; }
    public string? Estado { get; set; }
}
