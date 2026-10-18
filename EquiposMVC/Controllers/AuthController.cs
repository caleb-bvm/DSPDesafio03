using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class AuthController(AuthService auth) : AppController
{
    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString(SessionKeys.Token) is not null)
            return RedirectToAction("Index", "Equipo");
        return View(new LoginForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var result = await auth.Login(form);
        if (!result.Ok || result.Data is null)
        {
            ModelState.AddModelError("", result.Status == System.Net.HttpStatusCode.Unauthorized
                ? "Usuario o contraseña incorrectos."
                : "No fue posible iniciar sesión. Inténtalo de nuevo.");
            return View(form);
        }

        HttpContext.Session.SetString(SessionKeys.Token, result.Data.Token);
        HttpContext.Session.SetString(SessionKeys.Rol, result.Data.Rol);
        HttpContext.Session.SetString(SessionKeys.Username, result.Data.Username);
        return RedirectToAction("Index", "Equipo");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Register()
    {
        var access = CheckAccess("Admin");
        return access ?? View(new RegistroForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistroForm form)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;
        if (!ModelState.IsValid) return View(form);

        var result = await auth.Register(form);
        var error = HandleError(result, form: true);
        if (error is not null) return error;
        if (!result.Ok) return View(form);

        TempData["Success"] = $"Usuario {result.Data?.Username} creado.";
        return RedirectToAction("Index", "Equipo");
    }
}
