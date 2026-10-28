using System.Net;
using System.Security.Claims;
using EquiposReportes.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquiposReportes.Controllers;

// Inicio de sesión de Reportes. Las credenciales se validan contra la API
// (/api/auth/login); este proyecto no tiene usuarios propios.
public class CuentaController(IHttpClientFactory httpClientFactory) : Controller
{
    // GET /Cuenta/Login
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    // POST /Cuenta/Login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        // 1. Se envían usuario y contraseña a la API.
        HttpResponseMessage respuesta;
        try
        {
            var cliente = httpClientFactory.CreateClient("EquiposAPI");
            respuesta = await cliente.PostAsJsonAsync("api/auth/login", modelo);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo conectar con la API.");
            return View(modelo);
        }

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(modelo);
        }
        if (!respuesta.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "No se pudo iniciar sesión. Inténtalo de nuevo.");
            return View(modelo);
        }

        // 2. Solo el rol Admin puede entrar a los reportes.
        var datos = await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>();
        if (datos?.Rol != "Admin")
        {
            ModelState.AddModelError("", "Los reportes son exclusivos para administradores.");
            return View(modelo);
        }

        // 3. Se crea la cookie de sesión con el nombre y el rol. Vence junto con el token.
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, datos.Username),
            new Claim(ClaimTypes.Role, datos.Rol)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { ExpiresUtc = datos.ExpiresAt });

        return RedirectToAction("Index", "Reportes");
    }

    // POST /Cuenta/Salir
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // GET /Cuenta/AccesoDenegado
    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
