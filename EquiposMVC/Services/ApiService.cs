using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EquiposMVC.Services;

public static class SessionKeys
{
    public const string Token = "Token";
    public const string Rol = "Rol";
    public const string Username = "Username";
}

public class ApiTokenHandler(IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = contextAccessor.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}

public class ApiResult<T>
{
    public bool Ok { get; init; }
    public T? Data { get; init; }
    public HttpStatusCode Status { get; init; }
    public string Message { get; init; } = "";
}

public abstract class ApiService(HttpClient client)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected Task<ApiResult<T>> Get<T>(string path) => Send<T>(() => client.GetAsync(path));
    protected Task<ApiResult<T>> Post<T>(string path, object body) => Send<T>(() => client.PostAsJsonAsync(path, body));
    protected Task<ApiResult<T>> Put<T>(string path, object body) => Send<T>(() => client.PutAsJsonAsync(path, body));
    protected Task<ApiResult<bool>> Put(string path) => Send<bool>(() => client.PutAsync(path, null));
    protected Task<ApiResult<bool>> Delete(string path) => Send<bool>(() => client.DeleteAsync(path));

    private static async Task<ApiResult<T>> Send<T>(Func<Task<HttpResponseMessage>> request)
    {
        try
        {
            using var response = await request();
            if (!response.IsSuccessStatusCode)
            {
                var message = "No se pudo completar la solicitud.";
                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
                {
                    try
                    {
                        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
                        if (!string.IsNullOrWhiteSpace(error?.Message)) message = error.Message;
                    }
                    catch (JsonException) { }
                }
                return new ApiResult<T> { Status = response.StatusCode, Message = message };
            }

            if (typeof(T) == typeof(bool))
                return new ApiResult<T> { Ok = true, Status = response.StatusCode, Data = (T)(object)true };

            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return new ApiResult<T> { Ok = true, Status = response.StatusCode, Data = data };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Status = HttpStatusCode.ServiceUnavailable, Message = "El servicio no está disponible. Inténtalo de nuevo." };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T> { Status = HttpStatusCode.GatewayTimeout, Message = "El servicio tardó demasiado en responder. Inténtalo de nuevo." };
        }
        catch (JsonException)
        {
            return new ApiResult<T> { Status = HttpStatusCode.BadGateway, Message = "No se pudo leer la respuesta del servicio." };
        }
    }

    private class ApiError
    {
        public string? Message { get; set; }
    }
}
