using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EquiposAPI.Data;

public static class ErroresBaseDatos
{
    public static bool EsDuplicado(DbUpdateException error) =>
        error.GetBaseException() is SqlException sql && sql.Number is 2601 or 2627;

    public static bool EsRestriccion(DbUpdateException error) =>
        error.GetBaseException() is SqlException sql && sql.Number == 547;
}
