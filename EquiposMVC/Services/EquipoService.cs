using EquiposMVC.Models;

namespace EquiposMVC.Services;

public class EquipoService(HttpClient client) : ApiService(client)
{
    public Task<ApiResult<PaginaDto<EquipoDto>>> Listar(string? texto, int? categoriaId, string? estado, int page, int pageSize = 10)
    {
        var filtros = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(texto)) filtros.Add($"texto={Uri.EscapeDataString(texto)}");
        if (categoriaId.HasValue) filtros.Add($"categoriaId={categoriaId.Value}");
        if (!string.IsNullOrWhiteSpace(estado)) filtros.Add($"estado={Uri.EscapeDataString(estado)}");
        return Get<PaginaDto<EquipoDto>>($"api/equipos?{string.Join("&", filtros)}");
    }

    public Task<ApiResult<EquipoDto>> Obtener(int id) => Get<EquipoDto>($"api/equipos/{id}");
    public Task<ApiResult<EquipoDto>> Crear(EquipoForm form) => Post<EquipoDto>("api/equipos", form);
    public Task<ApiResult<EquipoDto>> Editar(int id, EquipoForm form) => Put<EquipoDto>($"api/equipos/{id}", form);
    public Task<ApiResult<bool>> Eliminar(int id) => Delete($"api/equipos/{id}");

    public async Task<ApiResult<List<EquipoDto>>> Disponibles()
    {
        var equipos = new List<EquipoDto>();
        var page = 1;
        while (true)
        {
            var result = await Listar(null, null, "Disponible", page, 100);
            if (!result.Ok || result.Data is null)
                return new ApiResult<List<EquipoDto>> { Status = result.Status, Message = result.Message };

            equipos.AddRange(result.Data.Items);
            if (page >= result.Data.TotalPages) break;
            page++;
        }

        return new ApiResult<List<EquipoDto>> { Ok = true, Data = equipos };
    }
}
