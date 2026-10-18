namespace EquiposAPI.Models;

public static class EstadoEquipo
{
    public const string Disponible = "Disponible";
    public const string Prestado = "Prestado";
    public const string Mantenimiento = "Mantenimiento";

    public static string? Normalizar(string? valor) =>
        new[] { Disponible, Prestado, Mantenimiento }
            .FirstOrDefault(estado => estado.Equals(valor?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Docente = "Docente";
    public const string Estudiante = "Estudiante";

    public static string? Normalizar(string? valor) =>
        new[] { Admin, Docente, Estudiante }
            .FirstOrDefault(rol => rol.Equals(valor?.Trim(), StringComparison.OrdinalIgnoreCase));
}
