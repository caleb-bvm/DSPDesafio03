namespace EquiposAPI.Models;

// Valores permitidos para Equipo.Estado. Se guardan como texto en la base.
public static class EstadoEquipo
{
    public const string Disponible = "Disponible";
    public const string Prestado = "Prestado";
    public const string Mantenimiento = "Mantenimiento";
}

// Roles de usuario. Se usan en [Authorize(Roles = ...)] y en el claim del token.
public static class Roles
{
    public const string Admin = "Admin";
    public const string Docente = "Docente";
    public const string Estudiante = "Estudiante";

    // Atajo para los endpoints que permiten Admin y Docente.
    public const string AdminODocente = "Admin,Docente";
}
