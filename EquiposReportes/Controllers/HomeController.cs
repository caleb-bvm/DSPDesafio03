using Microsoft.AspNetCore.Mvc;

namespace EquiposReportes.Controllers;

public class HomeController : Controller
{
    // Página de error (configurada en Program.cs): /Home/Error/404 para un código
    // HTTP y /Home/Error para una excepción. Un código que no es de error se trata como 404.
    public IActionResult Error(int? id)
    {
        var codigo = !RouteData.Values.ContainsKey("id") ? StatusCodes.Status500InternalServerError
            : id is >= 400 and <= 599 ? id.Value
            : StatusCodes.Status404NotFound;
        Response.StatusCode = codigo;
        return View(codigo);
    }
}
