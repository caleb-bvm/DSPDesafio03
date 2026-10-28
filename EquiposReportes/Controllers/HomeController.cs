using Microsoft.AspNetCore.Mvc;

namespace EquiposReportes.Controllers;

public class HomeController : Controller
{
    // Página genérica para errores inesperados (configurada en Program.cs).
    public IActionResult Error() => View();
}
