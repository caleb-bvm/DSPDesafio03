namespace EquiposAPI.Services;

public static class RelojLocal
{
    private static readonly TimeZoneInfo Zona = BuscarZona();

    public static DateTime Ahora() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateTime Hoy() => Ahora().Date;

    public static DateTime DiaEnGuatemala(DateTime fecha)
    {
        // Sin zona horaria, la fecha del formulario ya representa un día en Guatemala.
        if (fecha.Kind == DateTimeKind.Unspecified)
            return fecha.Date;

        return TimeZoneInfo.ConvertTime(fecha, Zona).Date;
    }

    private static TimeZoneInfo BuscarZona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        }
    }
}
