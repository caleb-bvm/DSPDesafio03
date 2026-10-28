using EquiposMVC.Models;

namespace EquiposMVC.Services;

// Llamadas a /api/prestamos
public class PrestamoService(HttpClient client) : ApiService(client)
{
    // No se envía ningún Id de usuario: la API decide qué préstamos ver según el token.
    public Task<ApiResult<List<PrestamoDto>>> Listar(bool soloVencidos = false) =>
        Get<List<PrestamoDto>>(soloVencidos ? "api/prestamos?soloVencidos=true" : "api/prestamos");

    public Task<ApiResult<PrestamoDto>> Crear(PrestamoForm form) => Post<PrestamoDto>("api/prestamos", form);
    public Task<ApiResult<PrestamoDto>> Devolver(int id) => Put<PrestamoDto>($"api/prestamos/{id}/devolucion");
}
