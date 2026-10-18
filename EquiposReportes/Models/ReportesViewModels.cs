namespace EquiposReportes.Models;

public class CategoriaOpcionViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class FiltroReporteViewModel
{
    public int? CategoriaId { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public string? Username { get; set; }
    public IReadOnlyList<CategoriaOpcionViewModel> Categorias { get; set; } = [];
}

public class PaginaReporteViewModel<TFila>
{
    public FiltroReporteViewModel Filtros { get; set; } = new();
    public IReadOnlyList<TFila> Filas { get; set; } = [];
    public string? Error { get; set; }
}

public class InventarioFilaViewModel
{
    public string Categoria { get; set; } = string.Empty;
    public int Disponibles { get; set; }
    public int Prestados { get; set; }
    public int EnMantenimiento { get; set; }
    public int Total => Disponibles + Prestados + EnMantenimiento;
}

public class VencidoFilaViewModel
{
    public string Username { get; set; } = string.Empty;
    public string CodigoInventario { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int DiasAtraso { get; set; }
}

public class TopEquipoFilaViewModel
{
    public string CodigoInventario { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int CantidadPrestamos { get; set; }
}

public class HistorialFilaViewModel
{
    public string Username { get; set; } = string.Empty;
    public string CodigoInventario { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
}
