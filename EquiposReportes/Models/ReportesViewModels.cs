namespace EquiposReportes.Models;

// Las vistas nunca reciben entidades de EF: solo estos ViewModels.

// Filtros que llegan por la URL: ?desde=&hasta=&categoriaId=&username=
public class FiltrosViewModel
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? CategoriaId { get; set; }
    public string? Username { get; set; }   // Solo lo usa el historial.

    // Opciones del <select> de categoría (no viene en la URL, lo llena el controlador).
    public List<CategoriaOpcionViewModel> Categorias { get; set; } = [];
}

public class CategoriaOpcionViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// Modelo de cada página de reporte: filtros + filas del resultado.
public class ReporteViewModel<TFila>
{
    public FiltrosViewModel Filtros { get; set; } = new();
    public List<TFila> Filas { get; set; } = [];
    public string? Error { get; set; }
}

// ----- Filas de cada reporte -----

// 1. Inventario por categoría
public class InventarioFilaViewModel
{
    public string Categoria { get; set; } = string.Empty;
    public int Disponibles { get; set; }
    public int Prestados { get; set; }
    public int EnMantenimiento { get; set; }
    public int Total => Disponibles + Prestados + EnMantenimiento;
}

// 2. Préstamos vencidos
public class VencidoFilaViewModel
{
    public string Username { get; set; } = string.Empty;
    public string CodigoInventario { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int DiasAtraso { get; set; }
}

// 3. Top 5 equipos más prestados
public class TopEquipoFilaViewModel
{
    public string CodigoInventario { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int CantidadPrestamos { get; set; }
}

// 4. Historial por usuario
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
