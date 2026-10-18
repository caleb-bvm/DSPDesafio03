using EquiposMVC.Models;

namespace EquiposMVC.Services;

public class PrestamoService(HttpClient client) : ApiService(client)
{
    public Task<ApiResult<List<PrestamoDto>>> Listar(bool soloVencidos = false) =>
        Get<List<PrestamoDto>>($"api/prestamos?soloVencidos={soloVencidos.ToString().ToLowerInvariant()}");

    public Task<ApiResult<PrestamoDto>> Crear(PrestamoForm form) => Post<PrestamoDto>("api/prestamos", form);
    public Task<ApiResult<bool>> Devolver(int id) => Put($"api/prestamos/{id}/devolucion");
}
