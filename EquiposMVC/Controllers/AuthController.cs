using System.Net;
using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class AuthController(AuthService auth) : AppController
{
    // GET /Auth/Login
    [HttpGet]
    public IActionResult Login()
    {
        // Si ya hay sesión, no tiene sentido mostrar el login.
        if (HttpContext.Session.GetString(SessionKeys.Token) is not null)
            return RedirectToAction("Index", "Equipo");

        return View(new LoginForm());
    }

    // POST /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var resultado = await auth.Login(form);
        if (!resultado.Ok)
        {
            ModelState.AddModelError("", resultado.Status == HttpStatusCode.Unauthorized
                ? "Usuario o contraseña incorrectos."
                : "No fue posible iniciar sesión. Inténtalo de nuevo.");
            return View(form);
        }

        // Se guardan token, rol y username en Session.
        HttpContext.Session.SetString(SessionKeys.Token, resultado.Data!.Token);
        HttpContext.Session.SetString(SessionKeys.Rol, resultado.Data.Rol);
        HttpContext.Session.SetString(SessionKeys.Username, resultado.Data.Username);

        return RedirectToAction("Index", "Equipo");
    }

    // POST /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    // GET /Auth/Usuarios  (solo Admin)
    [HttpGet]
    public async Task<IActionResult> Usuarios()
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await auth.Usuarios();
        if (!resultado.Ok) return PaginaDeError(resultado);

        return View(resultado.Data);
    }

    // GET /Auth/Register  (solo Admin)
    [HttpGet]
    public IActionResult Register()
    {
        return VerificarAcceso("Admin") ?? View(new RegistroForm());
    }

    // POST /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistroForm form)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;
        if (!ModelState.IsValid) return View(form);

        var resultado = await auth.Registrar(form);
        if (resultado.Ok)
        {
            TempData["Success"] = $"Usuario {resultado.Data!.Username} creado.";
            return RedirectToAction(nameof(Usuarios));
        }

        // 409 (username repetido) o 400: se muestra el mensaje y se conserva lo escrito.
        if (resultado.EsErrorDeFormulario)
        {
            ModelState.AddModelError("", resultado.Message);
            return View(form);
        }

        return PaginaDeError(resultado);
    }
}
