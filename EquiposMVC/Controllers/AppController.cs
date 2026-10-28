using System.Net;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

// Controlador base con lo que comparten todos los controladores:
// revisar la sesión y decidir qué mostrar cuando la API responde con error.
public abstract class AppController : Controller
{
    // Rol del usuario guardado en Session ("Admin", "Docente" o "Estudiante").
    protected string? Rol => HttpContext.Session.GetString(SessionKeys.Rol);

    // Revisa que el usuario haya iniciado sesión y, si se indican roles,
    // que tenga alguno de ellos. Devuelve null cuando puede continuar.
    // (La seguridad real está en la API; esto evita mostrar pantallas inútiles.)
    protected IActionResult? VerificarAcceso(params string[] roles)
    {
        if (HttpContext.Session.GetString(SessionKeys.Token) is null)
            return RedirectToAction("Login", "Auth");

        if (roles.Length > 0 && !roles.Contains(Rol))
            return View("AccessDenied");

        return null;
    }

    // Página que se muestra cuando la API responde con error.
    // (Los 400 y 409 de formularios se tratan antes, en cada acción, con ModelState.)
    protected IActionResult PaginaDeError(ApiResult resultado)
    {
        switch (resultado.Status)
        {
            // 401: el token venció o no es válido -> limpiar sesión e ir al login.
            case HttpStatusCode.Unauthorized:
                HttpContext.Session.Clear();
                TempData["Error"] = "Tu sesión venció. Inicia sesión de nuevo.";
                return RedirectToAction("Login", "Auth");

            // 403: tiene sesión, pero su rol no permite la acción.
            case HttpStatusCode.Forbidden:
                return View("AccessDenied");

            case HttpStatusCode.NotFound:
                return View("NotFound");

            // 5xx o API apagada: mensaje genérico, sin detalles internos.
            default:
                return View("Unavailable", "El servicio no está disponible. Inténtalo de nuevo más tarde.");
        }
    }
}
