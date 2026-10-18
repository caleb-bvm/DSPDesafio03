using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Equipo");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("~/Views/Shared/Unavailable.cshtml", "Ocurrió un error. Inténtalo de nuevo más tarde.");
    }
}
