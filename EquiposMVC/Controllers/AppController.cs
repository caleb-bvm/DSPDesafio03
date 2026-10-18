using System.Net;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public abstract class AppController : Controller
{
    protected IActionResult? CheckAccess(params string[] roles)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SessionKeys.Token)))
            return RedirectToAction("Login", "Auth");

        var rol = HttpContext.Session.GetString(SessionKeys.Rol);
        if (roles.Length > 0 && !roles.Contains(rol))
            return View("AccessDenied");

        return null;
    }

    // Devuelve una respuesta cuando hay que cambiar de página. En formularios, añade
    // el error al ModelState y devuelve null para conservar los datos escritos.
    protected IActionResult? HandleError<T>(ApiResult<T> result, bool form = false)
    {
        if (result.Ok) return null;

        if (result.Status == HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Clear();
            TempData["Error"] = "Tu sesión venció. Inicia sesión de nuevo.";
            return RedirectToAction("Login", "Auth");
        }

        if (result.Status == HttpStatusCode.Forbidden)
            return View("AccessDenied");

        if (form)
        {
            var message = (int)result.Status >= 500
                ? "El servicio no está disponible. Inténtalo de nuevo más tarde."
                : result.Message;
            ModelState.AddModelError("", message);
            return null;
        }

        if (result.Status == HttpStatusCode.NotFound)
            return View("NotFound");

        return View("Unavailable", (int)result.Status >= 500
            ? "El servicio no está disponible. Inténtalo de nuevo más tarde."
            : result.Message);
    }
}
