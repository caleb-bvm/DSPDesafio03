using System.Net.Http.Headers;

namespace EquiposMVC.Services;

// DelegatingHandler: se ejecuta antes de que salga cada petición HTTP hacia la API.
// Toma el token guardado en Session y lo agrega como "Authorization: Bearer <token>".
// Así ningún servicio tiene que acordarse de enviarlo.
public class ApiTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
