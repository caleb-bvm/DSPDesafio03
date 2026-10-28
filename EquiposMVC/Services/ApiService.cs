using System.Net;
using System.Text.Json;

namespace EquiposMVC.Services;

// Resultado de una llamada a la API: si salió bien, el código HTTP y el mensaje de error.
public class ApiResult
{
    public bool Ok { get; set; }
    public HttpStatusCode Status { get; set; }
    public string Message { get; set; } = "";

    // 400 y 409 son errores que el usuario puede corregir: se muestran dentro del formulario.
    public bool EsErrorDeFormulario => Status is HttpStatusCode.BadRequest or HttpStatusCode.Conflict;
}

// Igual que ApiResult, pero además trae los datos que devolvió la API.
public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}

// Clase base de los servicios. Hace la petición y convierte la respuesta en un ApiResult.
// El token NO se agrega aquí: lo agrega ApiTokenHandler automáticamente.
public abstract class ApiService(HttpClient client)
{
    protected Task<ApiResult<T>> Get<T>(string url) => Enviar<T>(client.GetAsync(url));
    protected Task<ApiResult<T>> Post<T>(string url, object datos) => Enviar<T>(client.PostAsJsonAsync(url, datos));
    protected Task<ApiResult<T>> Put<T>(string url, object datos) => Enviar<T>(client.PutAsJsonAsync(url, datos));
    protected Task<ApiResult<T>> Put<T>(string url) => Enviar<T>(client.PutAsync(url, null));
    protected Task<ApiResult<object>> Delete(string url) => Enviar<object>(client.DeleteAsync(url));

    private static async Task<ApiResult<T>> Enviar<T>(Task<HttpResponseMessage> peticion)
    {
        try
        {
            using var respuesta = await peticion;

            // Éxito (2xx): se leen los datos. 204 No Content no trae cuerpo.
            if (respuesta.IsSuccessStatusCode)
            {
                var datos = respuesta.StatusCode == HttpStatusCode.NoContent
                    ? default
                    : await respuesta.Content.ReadFromJsonAsync<T>();
                return new ApiResult<T> { Ok = true, Status = respuesta.StatusCode, Data = datos };
            }

            // Error: se guarda el código. En 400 y 409 la API manda { "message": "..." }.
            var resultado = new ApiResult<T> { Status = respuesta.StatusCode };
            if (resultado.EsErrorDeFormulario)
                resultado.Message = await LeerMensaje(respuesta) ?? "Revisa los datos enviados.";
            return resultado;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            // La API está apagada, tardó demasiado o respondió algo que no es JSON:
            // se trata como un 503 y el usuario ve el mensaje genérico.
            return new ApiResult<T> { Status = HttpStatusCode.ServiceUnavailable };
        }
    }

    // Lee el { "message": "..." } de un 400 o 409. Si el cuerpo no es JSON, devuelve null.
    private static async Task<string?> LeerMensaje(HttpResponseMessage respuesta)
    {
        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorApi>();
            return error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Forma del JSON de error que devuelve la API.
    private class ErrorApi
    {
        public string? Message { get; set; }
    }
}
