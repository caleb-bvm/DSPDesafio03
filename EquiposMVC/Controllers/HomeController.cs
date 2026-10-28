using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class HomeController : Controller
{
    // La página de inicio es el listado de equipos.
    public IActionResult Index() => RedirectToAction("Index", "Equipo");

    // Página genérica para errores inesperados (configurada en Program.cs).
    public IActionResult Error() =>
        View("~/Views/Shared/Unavailable.cshtml", "Ocurrió un error. Inténtalo de nuevo más tarde.");
}
