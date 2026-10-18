using System;
using System.Collections.Generic;

namespace EquiposReportes.Data.Entities;

public partial class Prestamo
{
    public int Id { get; set; }

    public int EquipoId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaPrestamo { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public DateTime? FechaDevolucion { get; set; }

    public string? Observaciones { get; set; }

    public virtual Equipo Equipo { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
