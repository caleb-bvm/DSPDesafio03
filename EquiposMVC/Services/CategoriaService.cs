using EquiposMVC.Models;

namespace EquiposMVC.Services;

// Llamadas a /api/categorias
public class CategoriaService(HttpClient client) : ApiService(client)
{
    public Task<ApiResult<List<CategoriaDto>>> Listar() => Get<List<CategoriaDto>>("api/categorias");
    public Task<ApiResult<CategoriaDto>> Obtener(int id) => Get<CategoriaDto>($"api/categorias/{id}");
    public Task<ApiResult<CategoriaDto>> Crear(CategoriaForm form) => Post<CategoriaDto>("api/categorias", form);
    public Task<ApiResult<CategoriaDto>> Editar(int id, CategoriaForm form) => Put<CategoriaDto>($"api/categorias/{id}", form);
    public Task<ApiResult<object>> Eliminar(int id) => Delete($"api/categorias/{id}");
}
