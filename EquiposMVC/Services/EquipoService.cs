using EquiposMVC.Models;

namespace EquiposMVC.Services;

// Llamadas a /api/equipos
public class EquipoService(HttpClient client) : ApiService(client)
{
    // Arma la URL con los filtros que tengan valor, por ejemplo:
    // api/equipos?page=1&pageSize=10&texto=dell&estado=Disponible
    public Task<ApiResult<PaginaDto<EquipoDto>>> Listar(
        string? texto, int? categoriaId, string? estado, int page, int pageSize = 10)
    {
        var url = $"api/equipos?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(texto)) url += $"&texto={Uri.EscapeDataString(texto)}";
        if (categoriaId.HasValue) url += $"&categoriaId={categoriaId}";
        if (!string.IsNullOrWhiteSpace(estado)) url += $"&estado={estado}";

        return Get<PaginaDto<EquipoDto>>(url);
    }

    // Equipos disponibles para el formulario de préstamo (máximo 100, el límite de la API).
    public Task<ApiResult<PaginaDto<EquipoDto>>> Disponibles() => Listar(null, null, "Disponible", 1, 100);

    public Task<ApiResult<EquipoDto>> Obtener(int id) => Get<EquipoDto>($"api/equipos/{id}");
    public Task<ApiResult<EquipoDto>> Crear(EquipoForm form) => Post<EquipoDto>("api/equipos", form);
    public Task<ApiResult<EquipoDto>> Editar(int id, EquipoForm form) => Put<EquipoDto>($"api/equipos/{id}", form);
    public Task<ApiResult<object>> Eliminar(int id) => Delete($"api/equipos/{id}");
}
