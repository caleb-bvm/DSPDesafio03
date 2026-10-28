using EquiposMVC.Models;

namespace EquiposMVC.Services;

// Llamadas a la API relacionadas con usuarios y login.
public class AuthService(HttpClient client) : ApiService(client)
{
    public Task<ApiResult<LoginResponse>> Login(LoginForm form) => Post<LoginResponse>("api/auth/login", form);
    public Task<ApiResult<UsuarioDto>> Registrar(RegistroForm form) => Post<UsuarioDto>("api/auth/register", form);
    public Task<ApiResult<List<UsuarioDto>>> Usuarios() => Get<List<UsuarioDto>>("api/usuarios");
}
